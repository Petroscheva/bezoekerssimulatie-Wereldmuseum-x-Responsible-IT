### CityPeople – Waypoint Navigation

Het `CityPeople`-script is uitgebreid zodat een personage nu langs meerdere waypoints kan lopen in plaats van naar één enkel target.

#### Belangrijkste codewijzigingen

- Vervangen van één target door een array van targets:
  - `GameObject _target` → `Transform[] _targets`
- Toegevoegd veld:
  - `int _currentTargetIndex` om bij te houden naar welk waypoint de agent onderweg is.
- In `Start()`:
  - Controle op aanwezigheid van een `NavMeshAgent`.
  - Controle of er minimaal één target (`_targets.Length > 0`) is ingesteld.
  - De eerste bestemming wordt gezet met  
    `_agent.SetDestination(_targets[_currentTargetIndex].position);`
- In `Update()`:
  - Er wordt gecontroleerd of de agent de huidige bestemming heeft bereikt
    (`remainingDistance <= stoppingDistance` en `!_agent.hasPath`).
  - Zodra de bestemming bereikt is, wordt `GoToNextTarget()` aangeroepen.
- Nieuwe methode `GoToNextTarget()`:
  - Verhoogt `_currentTargetIndex`.
  - Als er geen volgende target is, stopt de agent (optioneel kun je hier een loop van maken).

De bestaande functionaliteit voor animaties en paletten is ongewijzigd gebleven.

#### Hoe stel je de targets in (Unity)

1. Selecteer het GameObject met het `CityPeople`-script.
2. In de Inspector verschijnt het veld **Targets** (`Transform[] _targets`).
3. Zet de **Size** op het gewenste aantal waypoints (bijv. 3).
4. Maak in de scene voor elk waypoint een (Empty) GameObject op de gewenste positie.
5. Sleep de Transforms van deze GameObjects in de array, in de volgorde waarin het personage moet lopen:
   - Element 0 = eerste target
   - Element 1 = tweede target
   - Element 2 = derde target
   - etc.
6. Zorg dat op hetzelfde GameObject ook een `NavMeshAgent`-component aanwezig is en dat er een NavMesh in de scene is gebaked.

Na deze setup loopt het `CityPeople`-character automatisch van het eerste naar het laatste target en stopt vervolgens.
