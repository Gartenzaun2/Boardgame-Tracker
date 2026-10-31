using BoardgameTracker.Tools.Games.ArkNova.PhotoRecognition;

if (args.Length != 2 || args[0] is not ("prepare" or "fit" or "calibrate" or "evaluate" or "self-test"))
{
    Console.Error.WriteLine("Usage: <prepare|fit|calibrate|evaluate|self-test> <repository-root>");
    return 2;
}
return ClassicBatch.Run(args[0], Path.GetFullPath(args[1]));
