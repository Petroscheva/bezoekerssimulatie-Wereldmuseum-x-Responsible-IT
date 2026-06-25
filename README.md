# Wereldmuseum Bezoekersgedrag Simulatie
 
Een Unity-gebaseerde simulatie van bezoekersgedrag ontwikkeld voor het project **Wereldmuseum × Responsible IT**. Het systeem gebruikt echte observatiegegevens van bezoekers om te simuleren hoe museumbezoekers zich door tentoonstellingen bewegen, beslissen om ergens te stoppen en hoe lang zij objecten bekijken.
 
De simulatie is volledig data-gedreven, waardoor gedragsregels eenvoudig kunnen worden aangepast via een CSV-bestand zonder de code te wijzigen.
 
---
 
## Overzicht
 
Het project simuleert realistisch museumbezoek door middel van:
 
- Unity NavMesh-navigatie
- Waypoint-gebaseerde beweging
- Detectie van tentoonstellingsobjecten via triggerzones
- Data-gedreven besluitvorming
- Echte bezoekersobservaties
- Karakteranimaties
 
Wanneer een bezoeker een object of tentoonstelling tegenkomt, bepaalt het systeem op basis van observatiegegevens of de bezoeker stopt en hoe lang deze blijft kijken.
 
---
 
## Projectarchitectuur
 
De simulatie is opgebouwd rond drie kernscripts:
 
- `BehaviourDataLoader.cs`
- `BehaviourObjectId.cs`
- `CityPeople.cs`
 
---
 
# BehaviourDataLoader.cs
 
## Doel
 
Laadt observatiegegevens uit een CSV-bestand en zet deze om naar gedragsregels die door NPC-bezoekers kunnen worden gebruikt.
 
## Verantwoordelijkheden
 
- Inlezen van het CSV-bestand
- Uitlezen van object-ID's
- Verwerken van stopkansen
- Verwerken van kijktijden
- Opslaan van gedragsregels in het geheugen
- Beschikbaar maken van gedragsdata voor bezoekers
 
## Verwachte CSV-structuur
 
| Kolom | Beschrijving |
|---------|-------------|
| # | Object-ID |
| Stop Rate (%) | Kans dat een bezoeker stopt |
| Avg Wait per Person | Gemiddelde kijktijd |
 
### Voorbeeld
 
| # | Stop Rate (%) | Avg Wait per Person |
|---|---|---|
| 1 | 80% | 120 |
| 2 | 35% | 45 |
| 3 | 65% | 90 |
 
## Output
 
Elke rij wordt omgezet naar een gedragsregel met:
 
```csharp
objectId
stopChance
minStopSeconds
maxStopSeconds
```
 
Deze regels worden gebruikt wanneer bezoekers een object tegenkomen.
 
---
 
# BehaviourObjectId.cs
 
## Doel
 
Verbindt objecten in het museum met de juiste gedragsdata uit het CSV-bestand.
 
## Verantwoordelijkheden
 
Elk object krijgt een unieke identifier:
 
```csharp
public string objectId;
```
 
Deze ID moet overeenkomen met de ID in het CSV-bestand.
 
### Voorbeeld
 
CSV:
 
```text
# = 5
```
 
Object in Unity:
 
```text
objectId = "5"
```
 
Wanneer een bezoeker de triggerzone van het object betreedt, wordt deze ID gebruikt om de juiste gedragsregel op te halen.
 
## Configuratie
 
Voeg `BehaviourObjectId.cs` toe aan iedere triggerzone van een object en vul het juiste Object ID in.
 
---
 
# CityPeople.cs
 
## Doel
 
`CityPeople.cs` is het hoofdscript voor bezoekers. Het beheert beweging, interacties met objecten, animaties en gedragsbeslissingen.
 
Dit script vormt de verbinding tussen de navigatie in Unity en de gedragsdata van museumbezoekers.
 
---
 
## Karakterbeheer
 
Ondersteunt zowel mannelijke als vrouwelijke bezoekersmodellen.
 
Het script selecteert automatisch:
 
- Idle-animaties
- Loopanimaties
- Wachtanimaties
 
Voorbeelden:
 
```text
Vrouw:
- idle_f_2_190f
- locom_f_basicWalk_30f
 
Man:
- idle_m_2_220f
- locom_m_basicWalk_30f
```
 
---
 
## Waypoint Navigatie
 
Het `CityPeople`-script is uitgebreid zodat bezoekers langs meerdere waypoints kunnen bewegen in plaats van naar één enkele bestemming.
 
### Belangrijkste wijzigingen
 
#### Ondersteuning voor meerdere targets
 
```csharp
Transform[] _targets;
```
 
Hierdoor kan een bezoeker een vooraf gedefinieerde route door het museum volgen.
 
#### Huidige bestemming bijhouden
 
```csharp
int _currentTargetIndex;
```
 
Deze index houdt bij naar welk waypoint de bezoeker onderweg is.
 
#### Initialisatie in Start()
 
Bij het starten van de simulatie controleert het script:
 
- Of een `NavMeshAgent` aanwezig is
- Of minimaal één waypoint is ingesteld
 
Vervolgens wordt de eerste bestemming ingesteld:
 
```csharp
_agent.SetDestination(_targets[_currentTargetIndex].position);
```
 
#### Detecteren van een bereikt waypoint
 
In `Update()` wordt gecontroleerd of de bestemming bereikt is:
 
```csharp
_agent.remainingDistance <= _agent.stoppingDistance
```
 
en
 
```csharp
!_agent.hasPath
```
 
Wanneer beide voorwaarden waar zijn wordt automatisch:
 
```csharp
GoToNextTarget();
```
 
aangeroepen.
 
#### GoToNextTarget()
 
Deze methode:
 
1. Verhoogt `_currentTargetIndex`
2. Controleert of er nog een volgend waypoint bestaat
3. Stelt de nieuwe bestemming in
4. Stopt de agent wanneer het laatste waypoint is bereikt
 
---
 
## Detectie van Objecten
 
Wanneer een bezoeker een triggerzone binnenloopt:
 
```csharp
OnTriggerEnter()
```
 
voert het script de volgende stappen uit:
 
1. Detecteert het object
2. Leest het Object ID
3. Vraagt gedragsdata op bij `BehaviourDataLoader`
4. Berekent of de bezoeker stopt
 
---
 
## Gedragsbeslissingen
 
Met behulp van de CSV-data bepaalt het systeem:
 
```text
Moet de bezoeker stoppen?
```
 
Voorbeeld:
 
```text
Stop Rate = 80%
```
 
De bezoeker heeft dan 80% kans om te stoppen bij dat object.
 
---
 
## Kijktijd
 
Wanneer een bezoeker besluit te stoppen:
 
1. Wordt de beweging gepauzeerd
2. Speelt een wachtanimatie af
3. Blijft de bezoeker bij het object staan
4. Loopt de timer af
5. Hervat de bezoeker de route
 
De duur van het wachten wordt rechtstreeks uit de observatiegegevens gehaald.
 
---
 
## Geheugen voor Triggerzones
 
Om onrealistisch gedrag te voorkomen onthoudt het script welke objecten al bezocht zijn.
 
```csharp
HashSet<Collider> _ignoredTriggers
```
 
Hierdoor stopt een bezoeker niet telkens opnieuw bij hetzelfde object wanneer deze de triggerzone opnieuw passeert.
 
---
 
# Werking van het Systeem
 
```text
CSV Observatiegegevens
          │
          ▼
BehaviourDataLoader
          │
          ▼
Gedragsregels
          │
          ▼
CityPeople
          │
          ▼
Bezoeker betreedt triggerzone
          │
          ▼
BehaviourObjectId
          │
          ▼
Zoek bijbehorende gedragsregel
          │
          ▼
Bereken stopkans
          │
          ▼
Stop en bekijk object
          │
          ▼
Hervat route
```
 
---
 
# Installatie in Unity
 
## Stap 1 – Maak een Behaviour Manager
 
Maak een leeg GameObject aan:
 
```text
BehaviourManager
```
 
Voeg toe:
 
```text
BehaviourDataLoader
```
 
Koppel vervolgens het CSV-bestand in de Inspector.
 
---
 
## Stap 2 – Configureer Museumobjecten
 
Voor elk object:
 
1. Voeg een Collider toe
2. Schakel **Is Trigger** in
3. Voeg `BehaviourObjectId` toe
4. Vul het juiste Object ID in
 
Voorbeeld:
 
```text
Object ID = 12
```
 
---
 
## Stap 3 – Configureer Bezoekers
 
Voeg de volgende componenten toe:
 
```text
Animator
NavMeshAgent
CityPeople
```
 
Koppel vervolgens:
 
```text
Behaviour Data Loader
→ BehaviourManager
```
 
Configureer daarna:
 
```text
Targets
→ Waypoint Transforms
```
 
### Waypoints Instellen
 
1. Selecteer het GameObject met `CityPeople`.
2. Zoek het veld **Targets** in de Inspector.
3. Stel de grootte van de array in.
4. Maak voor ieder waypoint een leeg GameObject.
5. Sleep de Transforms in de juiste volgorde in de array.
 
Voorbeeld:
 
```text
Element 0 → Ingang
Element 1 → Tentoonstelling A
Element 2 → Tentoonstelling B
Element 3 → Uitgang
```
 
De bezoeker volgt deze route automatisch.
 
---
 
## Stap 4 – Bake de Navigatie
 
Open:
 
```text
Window → AI → Navigation
```
 
Bake vervolgens de NavMesh voor de museumvloer.
 
Controleer dat:
 
- Een NavMesh aanwezig is
- Alle bezoekers op een navigeerbaar oppervlak staan
- Een NavMeshAgent aanwezig is
 
---
 
# Voorbeeld van een Bezoekersroute
 
1. De bezoeker begint te lopen.
2. De bezoeker bereikt Object #3.
3. De triggerzone wordt geactiveerd.
4. `BehaviourObjectId` retourneert:
 
```text
objectId = 3
```
 
5. `BehaviourDataLoader` haalt op:
 
```text
Stopkans = 70%
Kijktijd = 90 seconden
```
 
6. De stopbeslissing slaagt.
7. De bezoeker stopt bij het object.
8. De kijktijd verloopt.
9. De bezoeker vervolgt de route.
 
---
 
# Data-Gedreven Ontwerp
 
Een belangrijk voordeel van het systeem is dat gedrag kan worden aangepast zonder codewijzigingen.
 
Om bezoekersgedrag te wijzigen:
 
1. Open het CSV-bestand
2. Pas stopkansen aan
3. Pas kijktijden aan
4. Start de simulatie opnieuw
 
Geen wijzigingen in de code zijn nodig.
 
---
 
# Mogelijke Uitbreidingen
 
- Verschillende bezoekerstypes
- Groepsgedrag
- Dynamische routeplanning
- Simulatie van bezoekersdrukte
- Heatmaps van bezoekersstromen
- Realtime analyse-dashboard
- Machine-learning voorspellingen
- Adaptieve populariteit van tentoonstellingen
 
---
 
# Vereisten
 
- Unity
- AI Navigation Package
- NavMeshAgent
- Animator Controller
- CSV-observatiegegevens
 
---
 
# Auteurs
 
Ontwikkeld als onderdeel van het project **Wereldmuseum × Responsible IT**.

**Rolverdeling:**

**Peter van de Geer** - Data verzamelen in het Wereldmuseum, Blender 3D model bouwen, waypoint navigatie scripting, navmeshsurface/agents aanmaken en aanpassen, colliders toevoegen en op de juiste plaats zetten.

**Tess Goossens** - Data verzamelen in het wereldmuseum, Excel sheet aanmaken, Script maken om de navigatie van navmeshagents te koppelen in Unity met de gedragsdata van museumbezoekers (CSV).

**Gloria Daniël** - Data verzamelen in het wereldmuseum.

**Eliza Wentzel** - Data verzamelen in het wereldmuseum.

De simulatie vertaalt echte observatiegegevens van museumbezoekers naar realistisch gedrag binnen een Unity-omgeving.
