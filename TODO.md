# TODO

Points notés en cours de route, pas traités tout de suite.

## Moteur (Gnoj-Ham-Library)

- [ ] `BasicCpuManagerPivot` : aucune anticipation (décisions purement locales/heuristiques) alors que le moteur est un simulateur parfait déjà disponible — une recherche Monte Carlo/expectimax à quelques coups d'avance pourrait renforcer le CPU sans aller jusqu'au RL/deep learning complet.
- [ ] `BasicCpuManagerPivot` : aucune prise en compte du furiten dans les décisions (défausse, riichi) — seule la règle est appliquée a posteriori au moment d'un Ron (`HandPivot.CancelYakusIfFuriten`). Fréquence réelle à mesurer (scan de parties simulées) avant de juger si un correctif est utile.

## Interface WPF (Gnoj-Ham-View)

- [ ] Pas de MVVM, accès aux contrôles par reconstruction de nom (`GraphicTools.FindName<T>`) — fragile aux renommages XAML ; `FindName` peut renvoyer `null` sans revérification (`GraphicTools.cs:150`).
