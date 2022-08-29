package Support_ClientDatabase
{
	function GameConnection::autoAdminCheck(%this)
	{
		%ret = parent::autoAdminCheck(%this);
		%this.openDatabase();
		return %ret;
	}

	function GameConnection::onDrop(%this)
	{
		%this.closeDatabase();
		if (isObject(%this.kvdb))
		{
			%this.kvdb.schedule(1000, delete);
		}
		parent::onDrop(%this);
	}
};
activatePackage(Support_ClientDatabase);

function GameConnection::openDatabase(%this)
{
	if (!isObject(%this.kvdb))
	{
		%this.kvdb = getUnqliteDB();
	}
	%this.kvdb.kv_open("config/server/ClientDatabases/" @ %this.bl_id @ ".db");
}

function GameConnection::closeDatabase(%this)
{
	if (!isObject(%this.kvdb))
	{
		return;
	}
	%this.kvdb.kv_close();
}

function GameConnection::setKV(%this, %key, %value)
{
	if (!%this.kvdb.open)
	{
		%this.openDatabase();
	}
	%this.kvdb.kv_set(%key, %value);
}

function GameConnection::getKV(%this, %key)
{
	if (!%this.kvdb.open)
	{
		%this.openDatabase();
	}
	%this.kvdb.kv_get(%key);
}

function GameConnection::deleteKV(%this, %key)
{
	if (!%this.kvdb.open)
	{
		%this.openDatabase();
	}
	%this.kvdb.kv_delete(%key);
}

function GameConnection::incKV(%this, %key, %incValue)
{
	%value = %this.getKV(%key);
	%this.setKV(%key, %value + %incValue);
}

function GameConnection::mulKV(%this, %key, %factor)
{
	%value = %this.getKV(%key);
	%this.setKV(%key, %value * %factor);
}





//@param	this	the client
//@param	input	tab-delimited key-value pairs, used to check if the purchase can be completed
//					value is positive if required, or negative if required and subtracted from inventory upon purchase
//@param	output	tab-delimited key-value pairs, added on successful purchase
//@return			1 if successful, 0 if not
function GameConnection::db_purchase(%this, %input, %output)
{
	%subCount = 0;
	for (%i = 0; %i < getFieldCount(%input); %i++)
	{
		%field = getField(%input, %i);
		%key = getWord(%field, 0);
		%required = getWord(%field, 1);

		if (%owned[%key] $= "")
		{
			%owned[%key] = %this.getKV(%key) + 0;
		}

		if (%owned[%key] < mAbs(%required))
		{
			return 0;
		}
		else if (%required < 0)
		{
			%subKey_[%subCount] = %key;
			%subAmt_[%subCount] = %required;
			%subCount++;
		}
	}

	//own enough of everything, now subtract + add output
	for (%i = 0; %i < %subCount; %i++)
	{
		%this.incKV(%subKey_[%i], %subAmt_[%i]);
	}

	for (%i = 0; %i < getFieldCount(%output); %i++)
	{
		%field = getField(%output, %i);
		%this.incKV(getWord(%field, 0), getWord(%field, 1));
	}
	return 1;
}