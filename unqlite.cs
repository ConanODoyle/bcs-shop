//flags:
//	open
//	dirty
//	autoCommit
//	closeOnQuit

if (!isObject($MainUnqliteDBSet))
{
	$MainUnqliteDBSet = new SimSet(MainUnqliteDBSet);
}
$Unqlite::PendingCommitTime = 5000;

function getUnqliteDB()
{
	%db = new ScriptObject(UnqliteDBs)
	{
		class = "UnqliteDB";
		open = false;
		dirty = false;
		autoCommit = true;
		closeOnQuit = true;
	};
	$MainUnqliteDBSet.add(%db);
	return %db;
}

function UnqliteDB::kv_open(%this, %path)
{
	if (%this.open)
	{
		return true;
	}

	%this.path = %path;
	%this.kvdb = unqlite_open(%path);
	if ($Unqlite::LastError != 0)
	{
		error("Unable to open KV store at \"", %path, "\"");
		%this.kvdb = "";
		return false;
	}
	%this.open = true;
	return true;
}

function UnqliteDB::kv_close(%this)
{
	if (!%this.open)
	{
		return true;
	}

	unqlite_close(%this.kvdb);
	if ($Unqlite::LastError != 0)
	{
		error("Unable to close KV store (kvdb ", %this.kvdb, ")");
		return false;
	}
	%this.kvdb = "";
	%this.open = false;
	return true;
}

function UnqliteDB::kv_get(%this, %key)
{
	%key = strLwr(%key);
	if (!%this.open)
	{
		error("Unable to get key - no database open");
		return "";
	}
	return unqlite_fetch(%this.kvdb, %key);
}

function UnqliteDB::kv_set(%this, %key, %value)
{
	%key = strLwr(%key);
	unqlite_store(%this.kvdb, %key, %value);
	if ($Unqlite::LastError != 0)
	{
		error("Unable to set key ", %key, " on kvdb ", %this.kvdb);
		return false;
	}
	%this.dirty = true;
	%this.kv_schedule_commit();
	return true;
}

function UnqliteDB::kv_delete(%this, %key)
{
	%key = strLwr(%key);
	unqlite_delete(%this.kvdb, %key);
	if ($Unqlite::LastError != 0 && $Unqlite::LastError != -6) //ignore key missing errors
	{
		error("Unable to delete key ", %key, " on kvdb ", %this.kvdb);
		return false;
	}
	%this.dirty = true;
	
	if (%this.autoCommit)
	{
		%this.kv_schedule_commit();
	}
	return true;
}

function UnqliteDB::kv_rollback(%this)
{
	unqlite_rollback(%this.kvdb);
	if ($Unqlite::LastError != 0) {
		error("Unable to rollback transaction on kvdb ", %this.kvdb);
		return false;
	}
	%this.dirty = false;
	%this.commitFailCount = 0;
	return true;
}

function UnqliteDB::kv_commit(%this)
{
	if (!%this.open || !%this.dirty)
	{
		return true;
	}

	cancel(%this.commitPending);
	unqlite_commit(%this.kvdb);
	if ($Unqlite::LastError !$= 0)
	{
		if (%this.commitFailCount < 2)
		{
			%this.commitFailCount++;
			error("Unable to commit kvdb ", %this.kvdb, " - attempting to commit again!");
			%this.kv_schedule_commit();
			return false;
		}
		else
		{
			error("Failed to commit 3 times - rolling back!");
			%this.kv_rollback();
			return false;
		}
	}
	%this.dirty = false;
	%this.commitFailCount = 0;
	return true;
}

function UnqliteDB::kv_schedule_commit(%this)
{
	cancel(%this.commitPending);
	%this.commitPending = %this.schedule($Unqlite::PendingCommitTime, "kv_commit");
}

function UnqliteDB::kv_export(%this, %file)
{
	unqlite_export(%this.kvdb, %file);
	if ($Unqlite::LastError != 0)
	{
		error("Failed to export KV database to \"", %file, "\" on kvdb ", %this.kvdb);
		return false;
	}
	return true;
}

package UnqliteDBPackage
{
	function UnqliteDB::onRemove(%this)
	{
		%this.kv_close();
		parent::onRemove(%this);
	}

	function onQuit()
	{
		for (%i = 0; %i < $MainUnqliteDBSet.getCount(); %i++)
		{
			%db = $MainUnqliteDBSet.getObject(%i);
			%db.kv_commit();
		}
		parent::onQuit();
	}

	function onExit()
	{
		for (%i = 0; %i < $MainUnqliteDBSet.getCount(); %i++)
		{
			%db = $MainUnqliteDBSet.getObject(%i);
			%db.kv_commit();
		}
		parent::onExit();
	}
};
activatePackage(UnqliteDBPackage);
