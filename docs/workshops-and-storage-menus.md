# House workshops and storage menus

Double-click the receiving chest to search and withdraw from every linked profession store, including nested bag contents. Individual profession chests and the Gatherer's Resource Satchel use the same menu. Set a withdrawal amount, then select an item. Deeds are displayed by their resource and quantity and withdraw whole. Normal container view remains available.

Collect item / bag targets your own inventory, your companion's nearby pack, or accessible storage in your house. It scans nested bags, moves matching loose items and leaves bags, unmatched items and inaccessible property intact. Each profession chest uses the actual sorting classifier; the satchel accepts its supported resources, gems, fish and bandages. Accepted items explains the categories. Withdrawals revalidate access and current item location; a full backpack preserves the stored quantity.

Eleven working craft stations are furnished in their appropriate workshops: smithing, tailoring, carpentry, fletching, tinkering, alchemy, cooking, inscription, glassblowing, masonry and cartography. They begin with 500 uses, accept ordinary matching tools up to a 5,000-use capacity, and do not disappear at zero charges. Runic and special tools are not consumed. Existing skill, recipe, material, forge/anvil and learned-technique requirements apply. Patch 0064 enables world-placed stations in the normal crafting checks; portable tools retain their usual rules.

The station behavior was adapted for ModernUO after examining ServUO's public Craft Addons implementation. This client's installed tiledata lacks the later machine graphics; the stations use existing workbench and craft-tool artwork. They are functional counterparts, not a claim that the later artwork was installed. Sources: https://github.com/ServUO/ServUO/tree/pub57/Scripts/Items/Addons/Craft%20Addons and https://uo.com/wiki/ultima-online-wiki/items/veteran-rewards/

A working mining cart and log-producing tree stump add supplies and detail. The patrol noticeboard now stands on a wooden platform with a dispatch crate, rope, chair and lantern; the old awkward shelter was removed. Storage references and the patrol board itself are preserved.

Validation: 1,110 content tests pass, including recursive collection, foreign-property rejection, partial withdrawal and rollback, master access to other chests, station access and refilling, all ladder approaches, and repeat furnishing.
