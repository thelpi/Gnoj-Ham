# TODO

Points notés en cours de route, pas traités tout de suite.

## Règles

- [ ] Yakuman "doubles" non modélisés (Suuankou tanki, Kokushi 13 tuiles, Chuuren Poutou pur) : tout yakuman vaut 13 fans fixe dans `YakuPivot.cs`.

## Moteur (Gnoj-Ham-Library)

- [ ] `RoundPivot`/`HandPivot` : classes trop grosses, plusieurs responsabilités mélangées.
- [ ] Pas de tests sur `YakuPivot.GetYakus` et `ScoreTools.GetFuCount`.
- [ ] `PlayerSavePivot` : sauvegarde JSON non chiffrée (`// TODO decrypt` déjà dans le code).
- [ ] `RoundPivot.cs:262` : `Game.HumanPlayerIndex!.Value` repose sur une discipline d'appel (humain uniquement) non garantie par le typage.

## Interface WPF (Gnoj-Ham-View)

- [ ] `MainWindow.InitializeAutoPlayWorker` : fuite d'abonnements aux événements du round (jamais désabonnés).
- [ ] `MainWindow.CancelCallProcess` : `Thread.Sleep` sur le thread UI.
- [ ] `RunWorkerCompleted` (MainWindow + AutoPlayWindow) : ignore `evt.Error`.
- [ ] Pas de MVVM, accès aux contrôles par reconstruction de nom (`GraphicTools.FindName<T>`) — fragile aux renommages XAML ; `FindName` peut renvoyer `null` sans revérification (`GraphicTools.cs:150`).
- [ ] `MainWindow.xaml.cs:649` : `CallKan(...)!` peut renvoyer `null`, pas revérifié juste avant le `!`.
- [ ] Options de l'onglet "Règles" (IntroWindow) non persistées entre lancements, contrairement à l'onglet "Options".
- [ ] `Gnoj-Ham-View/Notes.txt` : liste de bugs UI déjà existante, à fusionner ici un jour.
