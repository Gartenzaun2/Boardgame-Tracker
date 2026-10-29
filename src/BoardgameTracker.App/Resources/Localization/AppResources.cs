using System.Globalization;
using System.Resources;
using BoardgameTracker.Core.Players;

namespace BoardgameTracker.App.Resources.Localization;

/// <summary>
/// Provides strongly named access to localized UI strings used by the MAUI app.
/// </summary>
public static class AppResources
{
    private static readonly ResourceManager ResourceManager =
        new("BoardgameTracker.App.Resources.Localization.AppResources",
            typeof(AppResources).Assembly);

    public static string SpecialCardManagementTitle => GetString(nameof(SpecialCardManagementTitle));
    public static string SelectedSymbol => GetString(nameof(SelectedSymbol));
    public static string NewSpecialCard => GetString(nameof(NewSpecialCard));
    public static string EditSpecialCard => GetString(nameof(EditSpecialCard));
    public static string SpecialCardName => GetString(nameof(SpecialCardName));
    public static string ShowArchivedCards => GetString(nameof(ShowArchivedCards));
    public static string ArchiveCard => GetString(nameof(ArchiveCard));
    public static string UnarchiveCard => GetString(nameof(UnarchiveCard));
    public static string SpecialCardNameValidation => GetString(nameof(SpecialCardNameValidation));
    public static string SpecialCardOperationFailed => GetString(nameof(SpecialCardOperationFailed));
    public static string NoSpecialCards => GetString(nameof(NoSpecialCards));
    public static string SelectCardSymbol => GetString(nameof(SelectCardSymbol));
    public static string NoCardSymbol => GetString(nameof(NoCardSymbol));
    public static string SymbolPositionFormat => GetString(nameof(SymbolPositionFormat));
    public static string EditSpecialCardAction => GetString(nameof(EditSpecialCardAction));
    public static string CardSymbolAfrica => GetString(nameof(CardSymbolAfrica));
    public static string CardSymbolAmerica => GetString(nameof(CardSymbolAmerica));
    public static string CardSymbolAsia => GetString(nameof(CardSymbolAsia));
    public static string CardSymbolAustralia => GetString(nameof(CardSymbolAustralia));
    public static string CardSymbolEurope => GetString(nameof(CardSymbolEurope));
    public static string CardSymbolBear => GetString(nameof(CardSymbolBear));
    public static string CardSymbolBird => GetString(nameof(CardSymbolBird));
    public static string CardSymbolHerbivore => GetString(nameof(CardSymbolHerbivore));
    public static string CardSymbolPettingAnimal => GetString(nameof(CardSymbolPettingAnimal));
    public static string CardSymbolPredator => GetString(nameof(CardSymbolPredator));
    public static string CardSymbolPrimate => GetString(nameof(CardSymbolPrimate));
    public static string CardSymbolReptile => GetString(nameof(CardSymbolReptile));
    public static string CardSymbolSeaAnimal => GetString(nameof(CardSymbolSeaAnimal));
    public static string CardSymbolScience => GetString(nameof(CardSymbolScience));

    public static string ScoringInputHelpTitle => GetString(nameof(ScoringInputHelpTitle));
    public static string NumberWheelHint => GetString(nameof(NumberWheelHint));
    public static string SuggestedValue => GetString(nameof(SuggestedValue));
    public static string ScorePreview => GetString(nameof(ScorePreview));
    public static string NumberWheelConfirmationHint => GetString(nameof(NumberWheelConfirmationHint));

    public static string ArkNovaSettingsTitle => GetString(nameof(ArkNovaSettingsTitle));
    public static string CaptureReputation => GetString(nameof(CaptureReputation));
    public static string CaptureReputationHint => GetString(nameof(CaptureReputationHint));
    public static string OldScoringDisplay => GetString(nameof(OldScoringDisplay));
    public static string OldScoringDisplayHint => GetString(nameof(OldScoringDisplayHint));
    public static string Result => GetString(nameof(Result));

    public static string SetColorForGameFormat => GetString(nameof(SetColorForGameFormat));
    public static string NoPlayerColor => GetString(nameof(NoPlayerColor));

    public static string Back =>
        GetString(nameof(Back));

    public static string Groups =>
        GetString(nameof(Groups));

    public static string NoGroupsYet =>
        GetString(nameof(NoGroupsYet));

    public static string NewGroup =>
        GetString(nameof(NewGroup));

    public static string CreateFirstGroupHint =>
        GetString(nameof(CreateFirstGroupHint));

    public static string CreateGroup =>
        GetString(nameof(CreateGroup));

    public static string CreateGroupHint =>
        GetString(nameof(CreateGroupHint));

    public static string GroupName =>
        GetString(nameof(GroupName));

    public static string GroupNamePlaceholder =>
        GetString(nameof(GroupNamePlaceholder));

    public static string TemporaryGroup =>
        GetString(nameof(TemporaryGroup));

    public static string NewPlayer =>
        GetString(nameof(NewPlayer));

    public static string PlayerNamePlaceholder =>
        GetString(nameof(PlayerNamePlaceholder));

    public static string Guest =>
        GetString(nameof(Guest));

    public static string AddPlayer =>
        GetString(nameof(AddPlayer));


    public static string NoAvailablePlayers =>
        GetString(nameof(NoAvailablePlayers));


    public static string GroupPlayers =>
        GetString(nameof(GroupPlayers));

    public static string DefaultOrderHint =>
        GetString(nameof(DefaultOrderHint));

    public static string NoPlayersYet =>
        GetString(nameof(NoPlayersYet));


    public static string EnterPlayerName =>
        GetString(nameof(EnterPlayerName));


    public static string LoadPlayersFailed =>
        GetString(nameof(LoadPlayersFailed));

    public static string SaveGroupFailed =>
        GetString(nameof(SaveGroupFailed));

    public static string LoadGroupsFailed =>
        GetString(nameof(LoadGroupsFailed));

    public static string Save =>
        GetString(nameof(Save));

    public static string Ok =>
        GetString(nameof(Ok));

    public static string AddExistingPlayer =>
        GetString(nameof(AddExistingPlayer));

    public static string SelectPlayer =>
        GetString(nameof(SelectPlayer));

    public static string EditGroup =>
        GetString(nameof(EditGroup));

    public static string EditGroupHint =>
        GetString(nameof(EditGroupHint));

    public static string EditGroupPlayersHint =>
        GetString(nameof(EditGroupPlayersHint));

    public static string GroupNotFound =>
        GetString(nameof(GroupNotFound));

    public static string LoadGroupFailed =>
        GetString(nameof(LoadGroupFailed));

    public static string LastPlayedFormat =>
        GetString(nameof(LastPlayedFormat));

    public static string NeverPlayed =>
        GetString(nameof(NeverPlayed));

    public static string Initializing =>
        GetString(nameof(Initializing));

    public static string StartupErrorTitle =>
        GetString(nameof(StartupErrorTitle));

    public static string StartupErrorMessage =>
        GetString(nameof(StartupErrorMessage));

    public static string Retry =>
        GetString(nameof(Retry));

    public static string TechnicalDetails =>
        GetString(nameof(TechnicalDetails));

    public static string Temporary =>
        GetString(nameof(Temporary));

    public static string Archived =>
        GetString(nameof(Archived));

    public static string MakePermanent =>
        GetString(nameof(MakePermanent));

    public static string ArchiveHint =>
        GetString(nameof(ArchiveHint));

    public static string PrepareGame =>
        GetString(nameof(PrepareGame));

    public static string DetermineStartingPlayer =>
        GetString(nameof(DetermineStartingPlayer));

    public static string DrawZooPlans =>
        GetString(nameof(DrawZooPlans));

    public static string RotateOrder =>
        GetString(nameof(RotateOrder));

    public static string ChooseColor =>
        GetString(nameof(ChooseColor));

    public static string StartGame =>
        GetString(nameof(StartGame));

    public static string RunningGame =>
        GetString(nameof(RunningGame));

    public static string EndGame =>
        GetString(nameof(EndGame));

    public static string Discard =>
        GetString(nameof(Discard));

    public static string Scoring =>
        GetString(nameof(Scoring));

    public static string ScoringPlaceholder =>
        GetString(nameof(ScoringPlaceholder));

    public static string NoPreviousGameSession =>
        GetString(nameof(NoPreviousGameSession));

    public static string GroupNotFoundForPreparation =>
        GetString(nameof(GroupNotFoundForPreparation));

    public static string ArchivedGroupCannotStart =>
        GetString(nameof(ArchivedGroupCannotStart));

    public static string ArkNovaPlayerCountError =>
        GetString(nameof(ArkNovaPlayerCountError));

    public static string LoadPreparationFailed =>
        GetString(nameof(LoadPreparationFailed));

    public static string StartGameFailed =>
        GetString(nameof(StartGameFailed));

    public static string PreviousGameSessionFormat =>
        GetString(nameof(PreviousGameSessionFormat));

    public static string Blue =>
        GetString(nameof(Blue));

    public static string Yellow =>
        GetString(nameof(Yellow));

    public static string Red =>
        GetString(nameof(Red));

    public static string Black =>
        GetString(nameof(Black));

    public static string StartValidationTitle =>
        GetString(nameof(StartValidationTitle));

    public static string UniqueColorsRequired =>
        GetString(nameof(UniqueColorsRequired));

    public static string ZooPlanSelectionRequired =>
        GetString(nameof(ZooPlanSelectionRequired));

    public static string RunningGameSessionNotFound =>
        GetString(nameof(RunningGameSessionNotFound));

    public static string LoadRunningGameSessionFailed =>
        GetString(nameof(LoadRunningGameSessionFailed));

    public static string DurationHoursFormat =>
        GetString(nameof(DurationHoursFormat));

    public static string DurationMinutesFormat =>
        GetString(nameof(DurationMinutesFormat));

    public static string EndGameFailed =>
        GetString(nameof(EndGameFailed));

    public static string DiscardGameFailed =>
        GetString(nameof(DiscardGameFailed));

    public static string DiscardGameTitle =>
        GetString(nameof(DiscardGameTitle));

    public static string DiscardGameMessage =>
        GetString(nameof(DiscardGameMessage));

    public static string Cancel =>
        GetString(nameof(Cancel));

    public static string ZooPlanFormat =>
        GetString(nameof(ZooPlanFormat));

    public static string MoveUp =>
        GetString(nameof(MoveUp));

    public static string MoveDown =>
        GetString(nameof(MoveDown));

    public static string Remove =>
        GetString(nameof(Remove));

    public static string ArkNovaZooPlan01Name => GetString(nameof(ArkNovaZooPlan01Name));
    public static string ArkNovaZooPlan02Name => GetString(nameof(ArkNovaZooPlan02Name));
    public static string ArkNovaZooPlan03Name => GetString(nameof(ArkNovaZooPlan03Name));
    public static string ArkNovaZooPlan04Name => GetString(nameof(ArkNovaZooPlan04Name));
    public static string ArkNovaZooPlan05Name => GetString(nameof(ArkNovaZooPlan05Name));
    public static string ArkNovaZooPlan06Name => GetString(nameof(ArkNovaZooPlan06Name));
    public static string ArkNovaZooPlan07Name => GetString(nameof(ArkNovaZooPlan07Name));
    public static string ArkNovaZooPlan08Name => GetString(nameof(ArkNovaZooPlan08Name));
    public static string ArkNovaZooPlan09Name => GetString(nameof(ArkNovaZooPlan09Name));
    public static string ArkNovaZooPlan10Name => GetString(nameof(ArkNovaZooPlan10Name));
    public static string ArkNovaZooPlan11Name => GetString(nameof(ArkNovaZooPlan11Name));
    public static string ArkNovaZooPlan12Name => GetString(nameof(ArkNovaZooPlan12Name));
    public static string ArkNovaZooPlan13Name => GetString(nameof(ArkNovaZooPlan13Name));
    public static string ArkNovaZooPlan14Name => GetString(nameof(ArkNovaZooPlan14Name));

    public static string Games => GetString(nameof(Games));
    public static string History => GetString(nameof(History));
    public static string HistoryHint => GetString(nameof(HistoryHint));
    public static string Settings => GetString(nameof(Settings));
    public static string ArkNova => GetString(nameof(ArkNova));
    public static string SupportedGame => GetString(nameof(SupportedGame));
    public static string GameCatalogHint => GetString(nameof(GameCatalogHint));
    public static string AppSettingsTitle => GetString(nameof(AppSettingsTitle));
    public static string GameSettings => GetString(nameof(GameSettings));
    public static string GameSettingsHint => GetString(nameof(GameSettingsHint));
    public static string DefaultGame => GetString(nameof(DefaultGame));
    public static string NoDefaultGame => GetString(nameof(NoDefaultGame));
    public static string DefaultGameHint => GetString(nameof(DefaultGameHint));
    public static string ActiveGame => GetString(nameof(ActiveGame));
    public static string AddSpecialCard => GetString(nameof(AddSpecialCard));
    public static string SpecialCards => GetString(nameof(SpecialCards));
public static string ScoringSecondaryValuesFormat => GetString(nameof(ScoringSecondaryValuesFormat));
    public static string ScoringSecondaryValuesWithReputationFormat => GetString(nameof(ScoringSecondaryValuesWithReputationFormat));
    public static string SpecialCardAvailable => GetString(nameof(SpecialCardAvailable));
    public static string SpecialCardAlreadyAssigned => GetString(nameof(SpecialCardAlreadyAssigned));
    public static string SpecialCardAssignedHere => GetString(nameof(SpecialCardAssignedHere));
    public static string SpecialCardPickerTitleFormat => GetString(nameof(SpecialCardPickerTitleFormat));
    public static string SpecialCardMultipleTitle => GetString(nameof(SpecialCardMultipleTitle));
    public static string SpecialCardMultipleMessageFormat => GetString(nameof(SpecialCardMultipleMessageFormat));
    public static string Yes => GetString(nameof(Yes));
    public static string Close => GetString(nameof(Close));
    public static string DiscardScoringTitle => GetString(nameof(DiscardScoringTitle));
    public static string DiscardScoringMessage => GetString(nameof(DiscardScoringMessage));
    public static string DiscardScoringFailed => GetString(nameof(DiscardScoringFailed));

    public static string SaveCorrection => GetString(nameof(SaveCorrection));
    public static string CompleteScoring => GetString(nameof(CompleteScoring));
    public static string DeleteGameSession => GetString(nameof(DeleteGameSession));
    public static string DeleteGameSessionTitle => GetString(nameof(DeleteGameSessionTitle));
    public static string DeleteGameSessionMessage => GetString(nameof(DeleteGameSessionMessage));
    public static string HistoryGameFilter => GetString(nameof(HistoryGameFilter));
    public static string HistoryGroupFilter => GetString(nameof(HistoryGroupFilter));
    public static string HistoryAllGames => GetString(nameof(HistoryAllGames));
    public static string HistoryAllGroups => GetString(nameof(HistoryAllGroups));
    public static string HistoryEmpty => GetString(nameof(HistoryEmpty));
    public static string HistoryLoadFailed => GetString(nameof(HistoryLoadFailed));
    public static string HistoryOpenFailed => GetString(nameof(HistoryOpenFailed));
    public static string HistoryHeaderFormat => GetString(nameof(HistoryHeaderFormat));
    public static string HistoryDurationFormat => GetString(nameof(HistoryDurationFormat));
    public static string CorrectionInvalid => GetString(nameof(CorrectionInvalid));
    public static string CorrectionFailed => GetString(nameof(CorrectionFailed));
    public static string AttractivenessValidation => GetString(nameof(AttractivenessValidation));
    public static string ConservationValidation => GetString(nameof(ConservationValidation));
    public static string ReputationValidation => GetString(nameof(ReputationValidation));

    public static string ScoringNotFound => GetString(nameof(ScoringNotFound));
    public static string ScoringLoadFailed => GetString(nameof(ScoringLoadFailed));
    public static string ScoreNameFormat => GetString(nameof(ScoreNameFormat));
    public static string RawScoreFormat => GetString(nameof(RawScoreFormat));
    public static string PlaceFormat => GetString(nameof(PlaceFormat));
    public static string PointsFormat => GetString(nameof(PointsFormat));
    public static string TieInvalid => GetString(nameof(TieInvalid));
    public static string DragHere => GetString(nameof(DragHere));
    public static string CalculatedPoints => GetString(nameof(CalculatedPoints));
    public static string OptionalValue => GetString(nameof(OptionalValue));
    public static string OptionalReputation => GetString(nameof(OptionalReputation));
    public static string PhotoRecognition => GetString(nameof(PhotoRecognition));
    public static string PhotoRecognitionPending => GetString(nameof(PhotoRecognitionPending));

    public static string Done => GetString(nameof(Done));
    public static string Reputation => GetString(nameof(Reputation));
    public static string Attractiveness => GetString(nameof(Attractiveness));
    public static string Conservation => GetString(nameof(Conservation));

    public static string ResolveTies => GetString(nameof(ResolveTies));
    public static string ResolveTiesHint => GetString(nameof(ResolveTiesHint));
    public static string SaveScoring => GetString(nameof(SaveScoring));

    public static string AppearanceTheme => GetString(nameof(AppearanceTheme));
    public static string ThemeDeepForest => GetString(nameof(ThemeDeepForest));
    public static string ThemeRoyalBlue => GetString(nameof(ThemeRoyalBlue));
    public static string ThemeDark => GetString(nameof(ThemeDark));
    public static string HistoryFilterSelectedFormat => GetString(nameof(HistoryFilterSelectedFormat));
    public static string HistoryFilterFormat => GetString(nameof(HistoryFilterFormat));
    public static string ZooPlanSelected => GetString(nameof(ZooPlanSelected));

    public static string PlayerColorGreen => GetString(nameof(PlayerColorGreen));
    public static string PlayerColorWhite => GetString(nameof(PlayerColorWhite));
    public static string PlayerColorOrange => GetString(nameof(PlayerColorOrange));
    public static string PlayerColorBrown => GetString(nameof(PlayerColorBrown));
    public static string PlayerColorPurple => GetString(nameof(PlayerColorPurple));
    public static string PlayerColorPink => GetString(nameof(PlayerColorPink));
    public static string PlayerColorTurquoise => GetString(nameof(PlayerColorTurquoise));
    public static string PlayerColorGray => GetString(nameof(PlayerColorGray));

    public static string PreviousGameSection => GetString(nameof(PreviousGameSection));
    public static string GroupNameExample => GetString(nameof(GroupNameExample));
    public static string PlayerNameExample => GetString(nameof(PlayerNameExample));
    public static string PlayerNameLabel => GetString(nameof(PlayerNameLabel));
    public static string NewPlayerInline => GetString(nameof(NewPlayerInline));
    public static string AddExistingPlayerAction => GetString(nameof(AddExistingPlayerAction));

    public static string SelectExistingPlayerTitle => GetString(nameof(SelectExistingPlayerTitle));

    private static string GetString(string key) =>
        ResourceManager.GetString(key, CultureInfo.CurrentUICulture)
        ?? throw new InvalidOperationException(
            $"Localization resource '{key}' was not found.");
}
