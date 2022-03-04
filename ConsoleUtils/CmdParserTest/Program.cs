string[] testArgs = new string[] {  "cut" , "-16", "foo.dat"};

//string[] testArgs = new string[] { "flag" };

CmdParser cmdParser = new CmdParser(testArgs)
{
    { "cut", "c", CmdCommandTypes.VERB, new CmdParameters() {
        { CmdParameterTypes.INT, 0},
        { CmdParameterTypes.INT, 16},
    }, "Cut from here to there" },

    { "lines", "n", CmdCommandTypes.PARAMETER, CmdParameterTypes.INT, 10, "Line count for output" },
    { "flag", null, CmdCommandTypes.FLAG, true, "Flag" },
    { "file", "f", CmdCommandTypes.UNNAMED, "File to read" },

};

cmdParser.DefaultParameter = "file";

cmdParser.Parse();

var verbs = cmdParser.Verbs;

;

long offset = cmdParser["cut"].GetLong(0);
long length = cmdParser["cut"].GetLong(1);
long lines = cmdParser["lines"].GetLong(0);
bool flag = cmdParser["flag"].GetBool(0);
foreach(var file in cmdParser["file"].Strings)
{
    ;
}


;

