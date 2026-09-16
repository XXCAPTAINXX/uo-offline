param([string]$Voice='Microsoft Zira Desktop')
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Speech
$source=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'source/HavenBeaconQuest.cs') -Raw
$section=[regex]::Match($source,'(?s)VoiceLines=\{(.*?)\};').Groups[1].Value
$lines=[regex]::Matches($section,'"([^"]*)"')
if($lines.Count -ne 5){throw 'Expected five matching subtitle/voice lines.'}
$folder=Join-Path $PSScriptRoot 'assets/sounds/story'
New-Item -ItemType Directory -Path $folder -Force | Out-Null
$speaker=New-Object System.Speech.Synthesis.SpeechSynthesizer
try {
 $speaker.SelectVoice($Voice);$speaker.Rate=0;$speaker.Volume=85
 $format=New-Object System.Speech.AudioFormat.SpeechAudioFormatInfo(22050,[System.Speech.AudioFormat.AudioBitsPerSample]::Sixteen,[System.Speech.AudioFormat.AudioChannel]::Mono)
 for($i=0;$i -lt $lines.Count;$i++){
  $path=Join-Path $folder ('jenna-beacon-{0:00}.wav' -f ($i+1))
  $speaker.SetOutputToWaveFile($path,$format);$speaker.Speak($lines[$i].Groups[1].Value);$speaker.SetOutputToNull()
  Write-Output $path
 }
}finally{$speaker.Dispose()}
