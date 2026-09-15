"""Probe a local disposable preview login using its isolated TazUO settings.

Never print credentials or accept a non-loopback endpoint. This verifies the
native account-login/server-selection handshake, not rendering or gameplay.
"""
import argparse
import json
import os
import platform
import socket
import struct


def receive(sock, count):
    data = b""
    while len(data) < count:
        chunk = sock.recv(count - len(data))
        if not chunk:
            raise RuntimeError("Server closed the handshake")
        data += chunk
    return data


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("settings")
    args = parser.parse_args()
    with open(args.settings, encoding="utf-8-sig") as stream:
        settings = json.load(stream)
    if settings["ip"] != "127.0.0.1" or settings["port"] == 2593:
        raise RuntimeError("Only the separate loopback preview is allowed")
    encoded = settings["password"]
    if not encoded.startswith("1-"):
        raise RuntimeError("Expected this machine's isolated test-client settings")
    key = os.environ.get("COMPUTERNAME", platform.node()).encode("ascii")
    password = bytes(c ^ key[i % len(key)] for i, c in enumerate(bytes.fromhex(encoded[2:])))
    account = settings["username"].encode("ascii")
    if len(account) > 29 or len(password) > 29:
        raise RuntimeError("Fixture credentials exceed classic protocol limits")
    version = tuple(map(int, settings["clientversion"].split(".")))
    with socket.create_connection((settings["ip"], settings["port"]), timeout=10) as sock:
        sock.sendall(struct.pack(">BIIIII", 0xEF, 0x12345678, *version))
        sock.sendall(b"\x80" + account.ljust(30, b"\0") + password.ljust(30, b"\0") + b"\0")
        first = receive(sock, 1)
        if first != b"\xa8":
            raise RuntimeError("Account handshake rejected: response " + first.hex())
        length = struct.unpack(">H", receive(sock, 2))[0]
        payload = receive(sock, length - 3)
        count = struct.unpack(">H", payload[1:3])[0]
        if count < 1:
            raise RuntimeError("Server list is empty")
        index = struct.unpack(">H", payload[3:5])[0]
        sock.sendall(struct.pack(">BH", 0xA0, index))
        if receive(sock, 1) != b"\x8c":
            raise RuntimeError("Server selection did not return a game relay")
        relay = receive(sock, 10)
        port = struct.unpack(">H", relay[4:6])[0]
        if port != settings["port"]:
            raise RuntimeError("Relay points to an unexpected port")
    print("PASS local preview account login, populated server list and game relay")


if __name__ == "__main__":
    main()
