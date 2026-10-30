using BoardgameTracker.Tools.Games.ArkNova.PhotoRecognition;

if (args.Length != 4 || args[0] is not ("fit-b1" or "fit-b3" or "freeze" or "holdout" or "recovery-freeze" or "holdout-recovery" or "self-test"))
{
    Console.Error.WriteLine("Usage: <fit-b1|fit-b3|freeze|holdout|recovery-freeze|holdout-recovery|self-test> <repository-root> <dataset-v3> <report-directory>");
    return 2;
}
return ResearchBatch.Run(args[0], Path.GetFullPath(args[1]), Path.GetFullPath(args[2]), Path.GetFullPath(args[3]));
