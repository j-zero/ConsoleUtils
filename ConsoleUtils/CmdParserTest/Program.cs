//string[] testArgs = new string[] { "cut", "0x123", "32", "n", "5", "flag", "foo.dat" };

string[] testArgs = new string[] { "flag" };

CmdParser cmdParser = new CmdParser(testArgs)
{
    { "cut", "c", CmdCommandTypes.VERB, new CmdParameters() {
        { CmdParameterTypes.INT, 0},
        { CmdParameterTypes.INT, 16},
    }, "Cut from here to there" },
    
    { "lines", "n", CmdCommandTypes.PARAMETER, CmdParameterTypes.INT, 10, "Line count for output" },
    { "flag", null, CmdCommandTypes.FLAG, "Testflag" },
    { "file", "f", CmdCommandTypes.UNNAMED, "File to read" },

};

cmdParser.DefaultParameter = "file";

cmdParser.Parse();

int offset = cmdParser["cut"].GetInt(0);
int length = cmdParser["cut"].GetInt(1);
int lines = cmdParser["lines"].GetInt(0);
bool flag = cmdParser["flag"].GetBool(0);


;

