//largely taken from AoD, by Pecon

function strGetFirstAlphaIndex(%string)
{
	%length = strLen(%string);

	for(%i = 0; %i < %length; %i++)
	{
		%alphaChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
		%test = strReplace(%alphaChars, getSubStr(%string, %i, 1), "");
		if(strCmp(%test, %alphaChars) != 0)
			return %i;
	}
	return 0;
}

function strCapitalize(%string)
{
	%index = strGetFirstAlphaIndex(%string);

	return (%index > 0 ? getSubStr(%string, 0, %index) : "") @ strUpr(getSubStr(%string, %index, 1)) @ getSubStr(%string, %index + 1, strLen(%string) - 1);
}

// Remove special characters from the beginning and end of the string
function strTrimPunctuation(%string)
{
	%stripChars = "\'\".,!@#$%^&*()-_+=\\/[]{};:<>?`~|";

	if(strLen(%string) <= 1)
		return stripChars(%string, %stripChars);

	%check = stripChars(getSubStr(%string, 0, 1), %stripChars);
	if(%check $= "")
	{
		%string = getSubStr(%string, 1, strLen(%string) - 1);
		%string = strTrimPunctuation(%string);
	}

	%check = stripChars(getSubStr(%string, mClamp(strLen(%string) - 1, 0, 999999), 1), %stripChars);
	if(%check $= "")
	{
		%string = getSubStr(%string, 0, mClamp(strLen(%string) - 1, 0, 999999));
		%string = strTrimPunctuation(%string);
	}

	return %string;
}

// Format a string into mostly accurate title case.
function strTitleCase(%string)
{
	%count = getWordCount(%string);

	for(%i = 0; %i < %count; %i++)
	{
		%word = getWord(%string, %i);

		%testWord = strTrimPunctuation(%word);
		if((isPreposition(%testWord) || isArticle(%testWord) || isConjunction(%testWord)) && %i != 0)
			continue;

		%string = setWord(%string, %i, strCapitalize(%word));
	}

	return %string;
}

function isPreposition(%word)
{
	if($_preposition[trim(%word)])
		return true;
	else
		return false;
}

function isConjunction(%word)
{
	if($_conjunction[trim(%word)])
		return true;
	else
		return false;
}

function isArticle(%word)
{
	if($_article[trim(%word)])
		return true;
	else
		return false;
}

function generateWordArrays()
{
	deleteVariables("$_preposition*");
	%prepositions = "aboard about above absent across cross after against gainst along alongside amid amidst among amongst apropos apud around as astride at atop ontop bar before afore behind ahind below beneath neath besides beside between atween betwixt beyond but by chez circa come dehors despite spite down during except for from in inside into less like minus near nearer anear of off on onto opposite out outen outside over o'er pace past per plus post pre pro qua sans save sauf since sithence than through thru throughout thruout till to toward towards under underneath unlike until til unto up upon pon upside versus vs via vice vis-a-vis vis-\xE0-vis with w/ within w/i without w/o";
	%numPrepositions = getWordCount(%prepositions);

	for(%i = 0; %i < %numPrepositions; %i++)
		$_preposition[getWord(%prepositions, %i)] = true;

	deleteVariables("$_conjunction*");
	%conjunctions = "for and nor but or yet so either neither both whether the as rather not";
	%numConjunctions = getWordCount(%conjunctions);

	for(%i = 0; %i < %numConjunctions; %i++)
		$_conjunction[getWord(%conjunctions, %i)] = true;

	deleteVariables("$_article*");
	%articles = "a an the";
	%numArticles = getWordCount(%articles);

	for(%i = 0; %i < %numArticles; %i++)
		$_article[getWord(%articles, %i)] = true;
}
generateWordArrays();