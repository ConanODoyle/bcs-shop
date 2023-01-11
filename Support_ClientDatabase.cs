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
	%this.kvdb.kv_open("config/Unqlite/ClientDatabases/" @ %this.bl_id @ ".db");
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