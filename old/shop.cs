//@param	this	the client
//@param	input	tab-delimited key-value pairs, used to check if the purchase can be completed
//					value is positive if required, or negative if required and subtracted from inventory upon purchase
//@param	output	tab-delimited key-value pairs, added on successful purchase
//@return			1 if successful, 0 if not
function GameConnection::shop_purchase(%this, %input, %output)
{
	%subCount = 0;
	for (%i = 0; %i < getFieldCount(%input); %i++)
	{
		%field = getField(%input, %i);
		%key = getWord(%field, 0);
		%required = getWord(%field, 1);

		if (%owned[%key] $= "")
			%owned[%key] = %this.getKV(%key) + 0;

		if (%owned[%key] < mAbs(%required))
			return 0;
		else if (%required < 0)
		{
			%subKey_[%subCount] = %key;
			%subAmt_[%subCount] = %required;
			%subCount++;
		}
	}

	//own enough of everything, now subtract + add output
	for (%i = 0; %i < %subCount; %i++)
		%this.incKV(%subKey_[%i], %subAmt_[%i]);

	for (%i = 0; %i < getFieldCount(%output); %i++)
	{
		%field = getField(%output, %i);
		%this.incKV(getWord(%field, 0), getWord(%field, 1));
	}
	return 1;
}

//@param	this	the client
//@param	input	tab-delimited key-value pairs, used to check if the purchase can be completed
//					value is positive if required, or negative if required and subtracted from inventory upon purchase
//@return			keys which the client has insufficient value of
function GameConnection::shop_checkpurchase(%this, %input)
{
	%subCount = 0;
	for (%i = 0; %i < getFieldCount(%input); %i++)
	{
		%field = getField(%input, %i);
		%key = getWord(%field, 0);
		%required = getWord(%field, 1);

		if (%owned[%key] $= "")
			%owned[%key] = %this.getKV(%key) + 0;

		if (%owned[%key] < mAbs(%required))
			%keys = %keys SPC %key;
	}
	return trim(%keys);
}

function GameConnection::shop_owns(%this, %key)
{
	return (%this.getKV(%key) > 0 ? %this.getKV(%key) : 0);
}








//Database fields
//	item_count
//	item_[n]

//	[key]_exists
//	[key]_type
//	[key]_index = index where the key is stored
//	[key]_description

//	[type]_description

//	default_description

//	sale_[saleKey]_description
//	sale_[saleKey]_value = % off of item (eg 0.3 for 30% off)

if (!isObject($ShopDB))
{
	$ShopDB = getUnqliteDB();
}

if (!$ShopDB.open)
{
	$ShopDB.kv_open("config/server/ShopDatabase/main.db");
}

function confirmShopDBOpen()
{
	if (!$ShopDB.open)
	{
		$ShopDB.kv_open("config/server/ShopDatabase/main.db");
		if (!$ShopDB.open)
		{
			error("Unable to open shop database 'config/server/ShopDatabase/main.db'! Exiting...");
			return 0;
		}
	}
	return 1;
}


//@param	item	a unique item identifier (key)
//@param	type	a label that classifies the type of this item
//@param	force	boolean to skip existence errors and add the item + type anyways
//@return			1 if successful, 0 if failed
function registerShopItem(%item, %type, %force)
{
	if (%item $= "" || %type $= "")
	{
		error("Invalid empty parameters '" @ %item @ "' and/or '" @ %type @ "'!");
		return 0;
	}

	if (!confirmShopDBOpen())
		return 0;

	%key = %item;
	%keyExists = %key @ "_exists";
	%keyType = %key @ "_type";

	if ($ShopDB.kv_get(%keyExists))
	{
		%exists = 1;
		error("Item '" @ %item @ "' already exists! (key '" @ %key @ "')");
		if (!%force)
		{
			return 0;
		}
		echo("    Ignoring - reregistering item...");
	}

	if ((%currType = $ShopDB.kv_get(%keyType)) !$= %type && %currType !$= "")
		echo("    Overriding existing type '" @ %currType @ "' with '" @ %type @ "'!");

	//apply vars, add to list if needed
	if (!%exists)
	{
		%itemCount = $ShopDB.kv_get("item_count") + 0;
		%error += !$ShopDB.kv_set("item_" @ %itemCount, %key);
		%error += !$ShopDB.kv_set(%key @ "_index", %itemCount);
		%error += !$ShopDB.kv_set("item_count", %itemCount + 1);
		if (%error > 0) //check before applying exists value, otherwise it could not be on the list
		{
			error("    Some item fields for the list were not set! (key '" @ %key @ "')");
			return 0;
		}
		%error += !$ShopDB.kv_set(%keyExists, 1);
	}
	%error += !$ShopDB.kv_set(%keyType, %type);

	if (%error > 0)
	{
		error("    Some item fields were not set! (key '" @ %key @ "')");
		return 0;
	}
	return 1;
}


//@param	sale		a unique sale identifier (key)
//@param	description	the text representing the sale
//@param	value		% off of item (eg 0.3 for 30% off)
//@return			1 if successful, 0 if failed
function registerSale(%sale, %description, %value)
{
	if (%sale $= "" || %value $= "")
	{
		error("Invalid empty parameters '" @ %sale @ "' and/or '" @ %value @ "'!");
		return 0;
	}

	if (!confirmShopDBOpen())
		return 0;

	%saleDescription = %sale @ "_exists";
	%saleValue = %sale @ "_type";

	if ($ShopDB.kv_get(%saleValue) !$= "")
	{
		%exists = 1;
		error("Sale '" @ %sale @ "' already exists - reregistering...");
	}

	%error += !$ShopDB.kv_set(%saleDescription, %description);
	%error += !$ShopDB.kv_set(%saleValue, %value);

	if (%error > 0)
	{
		error("    Some sale fields were not set! (key '" @ %sale @ "')");
		return 0;
	}
	return 1;
}


//	Sets the description when attempting to purchase an item
//@param	item			a unique item identifier (key)
//@param	description		the text to put into the box when attempting a purchase
//@return					1 if successful, 0 if failed
function setShopItemDescription(%item, %description)
{
	if (%item $= "")
	{
		error("Invalid empty parameter '" @ %item @ "'!");
		return 0;
	}

	if (!confirmShopDBOpen())
		return 0;

	%key = %item;
	%keyExists = %key @ "_exists";
	%keyDescription = %key @ "_description";

	if (!$ShopDB.kv_get(%keyExists))
	{
		error("Item '" @ %item @ "' does not exist! Register the item first. (key '" @ %key @ "')");
		return 0;
	}

	if (%description $= "")
		%error += !$ShopDB.kv_set(%keyDescription, %description);
	else
		%error += !$ShopDB.kv_delete(%keyDescription);

	if (%error > 0)
	{
		error("    Some item fields were not set! (key '" @ %key @ "')");
		return 0;
	}
	return 1;
}


//	Sets the default description when attempting to purchase a type of item, if a specific one is not set
//	Supports replacers - see applyReplacers(%item, %description) for details
//@param	type			an item type
//@param	description		the text to put into the box when attempting a purchase
//@return					1 if successful, 0 if failed
function setTypeDefaultDescription(%type, %description)
{
	if (%type $= "")
	{
		error("Invalid empty parameter '" @ %type @ "'!");
		return 0;
	}

	if (!confirmShopDBOpen())
		return 0;

	%typeDescription = %type @ "_description";

	if (%description $= "")
		%error += !$ShopDB.kv_set(%typeDescription, %description);
	else
		%error += !$ShopDB.kv_delete(%typeDescription);

	if (%error > 0)
	{
		error("    Some item fields were not set! (key '" @ %type @ "')");
		return 0;
	}
	return 1;
}


//	Applies replacers to the input description. Case sensitive.
//	<item> - name of the item (%item itself)
//	<price> - price of the item
//	<sale> - sale text
//@param	description		the purchase box text
//@param	item			the item key
//@optional	sale			the sale key
//@return					a description with the contents replaced
function applyReplacers(%description, %item, %sale)
{
	%name = strTitleCase(%item);
	%price = $ShopDB.kv_get(%item @ "_price");
	if (%sale !$= "")
	{
		%saleText = $ShopDB.kv_get("sale_" @ %sale @ "_description");
		%saleValue = $ShopDB.kv_get("sale_" @ %sale @ "_value");
	}

	if (%saleValue > 0)
		%price = "\xa9" @ mFloor(%price * (1 - %saleValue)) @ " (" @ mFloor(%saleValue * 100) @ "% off!)";
	else
		%price = "\xa9" @ mFloor(%price);

	%description = strReplace(%description, "<item>", %name);
	%description = strReplace(%description, "<price>", %price);
	%description = strReplace(%description, "<sale>", %saletext);
	return %description;
}


//	Gets the description of an item
//@param	item			the item key
//@optional	sale			the sale key
//@return					a description with the contents replaced
function getItemDescription(%item, %sale)
{
	%key = %item;
	%keyExists = %key @ "_exists";
	%keyType = %key @ "_type";
	%keyDescription = %key @ "_description";

	if (!$ShopDB.kv_get(%keyExists))
	{
		error("Item '" @ %item @ "' does not exist! (key '" @ %key @ "')");
		return "";
	}

	// if ($ShopDB.itemDescription[getSafeVariableName(%item), getSafeVariableName(%sale)] !$= "")
	// 	return $ShopDB.itemDescription[getSafeVariableName(%item), getSafeVariableName(%sale)];

	%type = $ShopDB.kv_get(%keyType);
	%itemDescription = $ShopDB.kv_get(%keyDescription);
	%typeDescription = $ShopDB.kv_get(%type @ "_description");
	%defaultDescription = $ShopDB.kv_get("default_description");
	if (%itemDescription $= "" && %typeDescription $= "")
	{
		echo("Item '" @ %item @ "' and type '" @ %type @ "'has no description! Using default...");
		if (%defaultDescription $= "")
		{
			error("   No default description set!");
			return "";
		}
	}

	if (%itemDescription !$= "") %description = %itemDescription;
	else if (%typeDescription !$= "") %description = %typeDescription;
	else if (%defaultDescription !$= "") %description = %defaultDescription;

	%ret = applyReplacers(%description, %item, %sale);
	//memoize for faster repeat pullup?
	// $ShopDB.itemDescription_[getSafeVariableName(%item), getSafeVariableName(%sale)] = %ret;
	return %ret;
}