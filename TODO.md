# TODO

Points notés en cours de route, pas traités tout de suite.

## Moteur (Gnoj-Ham-Library)

- [ ] `BasicCpuManagerPivot` : aucune anticipation (décisions purement locales/heuristiques) alors que le moteur est un simulateur parfait déjà disponible — une recherche Monte Carlo/expectimax à quelques coups d'avance pourrait renforcer le CPU sans aller jusqu'au RL/deep learning complet. Tenté sur `feature/monte-carlo-cpu` (rollouts + déterminisation), abandonné : trop lent, gains peu concluants sur petit échantillon.
- [ ] `BasicCpuManagerPivot` : parmi les défausses qui gardent le tenpai, aucun critère ne préfère une attente large (ryanmen) dont les tuiles sont encore vivantes à une attente étroite déjà en grande partie morte — ajouter `deadTiles` comme tie-break avant la distance au centre dans `GetBestDiscardFromList`.
- [ ] `BasicCpuManagerPivot` : la lecture "mur" (kabe : 3-4 exemplaires d'une tuile déjà visibles ⇒ beaucoup plus sûre) n'existe que pour les honneurs isolés (`IsGuaranteedSafe`) — l'étendre aux tuiles numériques dans `ComputeTilesSafety`.
- [ ] `BasicCpuManagerPivot` : le riichi est systématique dès l'éligibilité, sans réévaluation (damaten si yaku déjà garanti + attente faible) — plus discutable qu'un simple ajustement de bord, à valider prudemment (pas juste par winrate agrégé).
## Interface WPF (Gnoj-Ham-View)

- [ ] Pas de MVVM, accès aux contrôles par reconstruction de nom (`GraphicTools.FindName<T>`) — fragile aux renommages XAML ; `FindName` peut renvoyer `null` sans revérification (`GraphicTools.cs:150`).
