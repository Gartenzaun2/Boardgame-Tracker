using BoardgameTracker.Tools.Games.ArkNova.PhotoRecognition;

if (args.Length != 2 || args[0] is not ("prepare" or "calibrate" or "compare" or "self-test"))
{
    Console.Error.WriteLine("Usage: <prepare|calibrate|compare|self-test> <repository-root>");
    return 2;
}
return C3FocusedBatch.Run(args[0], Path.GetFullPath(args[1]));
