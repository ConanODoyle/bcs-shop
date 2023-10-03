// inventory system:
// items can be added and removed
// items are individual - they can have custom values
// items have a base type, for maximum count purposes
// inventories have a maximum size, which can be infinite
// inventories can hold a limited # of an item type; applies to all or can be specific
// inventories are unique to each blid
// inventories have order and new elements are appended
// inventory lists can be retrieved in expanded or collapsed format (collapsed by type)?

$maxItemTypeCount = -1; //baseline maximum number of an item type one can own, -1 for infinite
$maxInventorySlots = 999999; //maximum number of inventory slots

function GameConnection::loadInventory(%cl)
{
	%cl.openDatabase();
	%cl.applyInventoryLimits();
}

//returns IDX of the item
function GameConnection::addItemToInventory(%cl, %baseType)
{
	%cl.openDatabase();
	%count = %cl.getKV("inventoryItemCount") + 0;
	%max = %cl.getKV("inventoryMaxCount") + 0;

	if (%count == %max)
	{
		return -1;
	}

	%cl.setKV("inventory_slot" @ %count, %baseType);
	%cl.setKV("inventoryItemCount", %count + 1);
	return %count;
}

function GameConnection::removeItemIDXFromInventory(%cl, %idx)
{
	%cl.openDatabase();
	for (%i = 0; %i < %count; %i++)
	{
		shuffleItemDown(%cl, %i)
	}
}