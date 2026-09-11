# TODO

Points notés en cours de route, pas traités tout de suite.

## Règles

- [ ] Yakuman "doubles" non modélisés (Suuankou tanki, Kokushi 13 tuiles, Chuuren Poutou pur) : tout yakuman vaut 13 fans fixe dans `YakuPivot.cs`.

## Moteur (Gnoj-Ham-Library)

- [ ] `RoundPivot` : classe trop grosse, plusieurs responsabilités mélangées (pas de `partial class`, à voir autrement). `HandPivot` traité (combinatoire extraite vers `TileCombinatoricsPivot`).
- [ ] Pas de tests sur les yaku dépendants du contexte (pas d'`Example` statique) : Riichi, Ippatsu, Tenhou, Chiihou, Renhou, Haitei, Rinshan Kaihou, Chankan, Menzen Tsumo, Daburu Riichi, Sanankou, Sankantsu, Suukantsu. Les 26 autres yaku sont couverts (`YakuPivot_Tests.cs`).
- [ ] `PlayerSavePivot` : sauvegarde JSON non chiffrée (`// TODO decrypt` déjà dans le code).
- [ ] `RoundPivot.cs:262` : `Game.HumanPlayerIndex!.Value` repose sur une discipline d'appel (humain uniquement) non garantie par le typage.

## Interface WPF (Gnoj-Ham-View)

- [ ] `BackgroundWorker` (MainWindow + AutoPlayWindow) daté, à migrer vers `async`/`await` + `Task` — chantier à part, risque de régression, pas un fix ponctuel.
- [ ] Pas de MVVM, accès aux contrôles par reconstruction de nom (`GraphicTools.FindName<T>`) — fragile aux renommages XAML ; `FindName` peut renvoyer `null` sans revérification (`GraphicTools.cs:150`).
- [ ] `MainWindow.xaml.cs:649` : `CallKan(...)!` peut renvoyer `null`, pas revérifié juste avant le `!`.
- [ ] Options de l'onglet "Règles" (IntroWindow) non persistées entre lancements, contrairement à l'onglet "Options".
- [ ] `Gnoj-Ham-View/Notes.txt` : liste de bugs UI déjà existante, à fusionner ici un jour.
