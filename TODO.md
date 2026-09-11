# TODO

Points notés en cours de route, pas traités tout de suite.

## Règles

- [ ] Yakuman "doubles" non modélisés (Suuankou tanki, Kokushi 13 tuiles, Chuuren Poutou pur) : tout yakuman vaut 13 fans fixe dans `YakuPivot.cs`.

## Moteur (Gnoj-Ham-Library)

- [ ] `RoundPivot`/`HandPivot` : classes trop grosses, plusieurs responsabilités mélangées.
- [ ] Pas de tests sur `YakuPivot.GetYakus` et `ScoreTools.GetFuCount`.
- [ ] `PlayerSavePivot` : sauvegarde JSON non chiffrée (`// TODO decrypt` déjà dans le code).

## Interface WPF (Gnoj-Ham-View)

- [ ] `MainWindow.InitializeAutoPlayWorker` : fuite d'abonnements aux événements du round (jamais désabonnés).
- [ ] `MainWindow.CancelCallProcess` : `Thread.Sleep` sur le thread UI.
- [ ] `RunWorkerCompleted` (MainWindow + AutoPlayWindow) : ignore `evt.Error`.
- [ ] Pas de MVVM, accès aux contrôles par reconstruction de nom (`GraphicTools.FindName<T>`) — fragile aux renommages XAML.
- [ ] Options de l'onglet "Règles" (IntroWindow) non persistées entre lancements, contrairement à l'onglet "Options".
- [ ] `Gnoj-Ham-View/Notes.txt` : liste de bugs UI déjà existante, à fusionner ici un jour.
