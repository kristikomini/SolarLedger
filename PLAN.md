# SolarLedger — Piano di progetto
### Mini piattaforma di settlement per Comunità Energetiche Rinnovabili (CER)

> Nome di lavoro: **SolarLedger** (energia + il "ledger", cioè la contabilità di chi
> riceve cosa — che è il cuore tecnico). Alternative: *EnergiaCondivisa*, *GridShare*,
> *CERtus*. Cambialo pure.

**Perché questo progetto, adesso.** Il colloquio Teoresi ha trovato due buchi: **Oracle**
(mai usato) e le **applicazioni multi-server** (coordinamento tra istanze). Questo
progetto li chiude tutti e due di proposito, restando nel dominio Energy in cui sei già
forte. Non è un progetto a caso: è la risposta costruita alle due domande che non hai
saputo chiudere.

---

## 1. Cosa fa una CER, in breve (il dominio da preservare)

Una Comunità Energetica è un gruppo di membri (case, un comune, piccole imprese) sotto
la stessa cabina primaria. Alcuni **producono** energia (di solito fotovoltaico), tutti
**consumano**. Il meccanismo economico è questo:

1. Ora per ora si misura quanta energia la comunità **immette** in rete e quanta ne
   **preleva**.
2. L'**energia condivisa** di quell'ora è il **minimo tra immessa e prelevata**: è la
   quota prodotta e consumata *nella stessa ora* dalla comunità.
3. Il GSE paga un **incentivo** (una tariffa in €/MWh) **solo su quell'energia
   condivisa**.
4. L'incentivo viene **ripartito** tra i membri secondo una policy decisa dalla
   comunità (di solito una parte ai produttori per ripagare l'impianto, una parte ai
   consumatori, a volte una quota a un fondo sociale).
5. La quota di ciascun membro diventa **credito in bolletta** o un bonifico.

> **La formula chiave, quella da non semplificare mai:**
> `energia_condivisa(ora) = min( Σ immesso(ora), Σ prelevato(ora) )`
> sommata su tutte le ore del periodo. È la logica dell'autoconsumo condiviso, ed è
> letteralmente il motivo per cui la CER esiste. Tutto il resto le gira intorno.

⚠️ **Nota di onestà:** la tariffa reale del GSE non è un numero fisso, è una formula che
dipende dalla potenza dell'impianto e dal prezzo zonale, più una componente ARERA di
valorizzazione. Nel progetto la modelli come **tariffa configurabile** e lo dichiari:
non presentare numeri come ufficiali, il punto del progetto è la *logica*, non la
conformità normativa esatta.

---

## 2. Cosa tieni e cosa tagli (mini, ma fedele)

**TIENI** — è qui che sta il "same logic and principles":
- Ingestione di letture **orarie** per POD.
- Il calcolo dell'**energia condivisa oraria** con la formula del minimo.
- Il calcolo dell'**incentivo** sulla condivisa.
- La **ripartizione** tra i membri con policy sostituibile.
- Il **settlement run** periodico (mensile) → estratto conto per membro.
- L'**idempotenza**: ri-eseguire il settlement di un periodo non deve pagare due volte.

**TAGLIA** — non servono per il CV e sono solo peso normativo:
- Integrazione reale con le API del GSE → produci solo un **report** (CSV).
- Fatturazione elettronica / SDI.
- Tariffe variabili nel tempo, cabine multiple, più configurazioni → **una comunità,
  una tariffa**.
- Autenticazione completa → uno stub, o niente.
- Dati a 15 minuti → **solo orari**.
- Derivare l'immesso dal lordo meno l'autoconsumo dietro contatore → l'immesso e il
  prelevato li ricevi già misurati per POD.

---

## 3. Modello dati (6 entità — minimo ma completo)

```
Community (la configurazione CER)
  Id, Nome, CodiceCabinaPrimaria, TariffaIncentivo (€/MWh), PolicyRiparto
        │
        ├──< Member (membro)
        │      Id, Nome, Ruolo {Produttore|Consumatore|Prosumer},
        │      RiferimentoPagamento (IBAN/bolletta), CommunityId
        │            │
        │            └──< Pod (punto di connessione)
        │                   Id, CodicePod, Tipo {Produzione|Consumo|Entrambi}, MemberId
        │
        └──< SettlementRun (esecuzione del settlement su un periodo)
               Id, CommunityId, PeriodoInizio, PeriodoFine,
               Stato {InCorso|Completato|Fallito}, EseguitoIl,
               TotaleCondivisaKwh, TotaleIncentivo
                     │
                     ├──< HourlyShare (dettaglio orario, per audit)   ← figlio del run
                     │      Ora, TotImmesso, TotPrelevato, CondivisaKwh
                     │
                     └──< MemberSettlement (la riga di pagamento per membro)
                            MemberId, CondivisaAttribuitaKwh,
                            ImportoIncentivo, CreditoApplicato

EnergyReading (la serie storica grezza)   ← tabella a sé, la più grande
  Id, CodicePod, Ora (timestamp orario), ImmessoKwh, PrelevatoKwh
```

Sei tabelle "vere" (`Community`, `Member`, `Pod`, `SettlementRun`, `MemberSettlement`,
`EnergyReading`) più `HourlyShare` come figlia del run per l'audit. Chiave naturale
utile su cui ragionare a colloquio: `EnergyReading` per `(CodicePod, Ora)` unico — e
`SettlementRun` unico per `(CommunityId, PeriodoInizio, PeriodoFine)`, che è ciò che
rende il settlement idempotente.

---

## 4. Il motore di settlement (il cuore — la parte da curare)

Pseudocodice dell'esecuzione su un periodo:

```
1. Segna il run come InCorso (o rifiuta se ne esiste già uno Completato per lo stesso
   periodo → idempotenza).
2. Per ogni ORA del periodo:
     immesso   = Σ ImmessoKwh   dei POD della comunità in quell'ora
     prelevato = Σ PrelevatoKwh dei POD della comunità in quell'ora
     condivisa = min(immesso, prelevato)
     salva HourlyShare(ora, immesso, prelevato, condivisa)
3. totaleCondivisa = Σ condivisa
   totaleIncentivo = totaleCondivisa (in MWh) × TariffaIncentivo
4. Applica la PolicyRiparto per attribuire a ciascun membro la sua quota:
     - ai produttori pro-quota sull'immesso nelle ore con condivisione
     - ai consumatori pro-quota sul prelevato nelle ore con condivisione
     - (eventuale quota a fondo comunità)
   → una riga MemberSettlement per membro
5. Segna il run Completato in transazione con le righe.
```

**Dettagli fedeli da non perdere** (sono ciò che distingue "so cos'è una CER" da "ho
fatto un CRUD"):
- L'attribuzione al singolo membro ha bisogno di una **chiave di riparto**: la
  condivisa è un aggregato di comunità, non è del singolo. Modellala esplicitamente.
- La **PolicyRiparto** è una *strategia sostituibile* (interfaccia + implementazioni):
  "tutto ai produttori", "50/50 produttori-consumatori", "con fondo sociale". Bella da
  mostrare e banale da testare.
- L'**idempotenza** è il requisito serio: è denaro. Ri-eseguire = stesso risultato,
  mai doppio pagamento. O rifiuti il ri-run, o cancelli-e-ricalcoli dentro una
  transazione.

**Testing (qui prendi punti veri):** la logica di riparto è deterministica → unit test
con xUnit sui casi limite:
- ora senza produzione (condivisa = 0)
- ora con produzione > consumo (condivisa limitata dal prelievo)
- ora con consumo > produzione (condivisa limitata dall'immesso)
- membro senza POD, periodo senza letture, ri-esecuzione (idempotenza)

---

## 5. Stack — **allineato all'annuncio Teoresi, di proposito**

| Scelta | Perché |
|---|---|
| **.NET 8** (non 10) | L'annuncio dice ".NET 8". Match esatto. |
| **C# + ASP.NET Core Web API** | Il tuo core. |
| **Oracle XE 21c in Docker** + **EF Core 8** con `Oracle.EntityFrameworkCore` | ⭐ Chiude il buco Oracle. Da "non l'ho mai usato" a "l'ho usato in un progetto". |
| **Settlement come background worker** | Chiude il buco multi-server (vedi §6). |
| **xUnit** + **Testcontainers (modulo Oracle)** | Unit sul motore, integrazione su Oracle vero e usa-e-getta. |
| **Docker Compose** (api + oracle) | Reggi il "docker compose up funziona", come su E-commerce. |
| **GitHub Actions** | CI con build + test, come già fai. |
| Health check + un po' di OpenTelemetry | Riusa i pattern di LogiFlow, leggeri. |
| *(stretch)* mini dashboard **React** | L'estratto conto mensile di un membro. Gioca il full-stack. Opzionale. |

⚠️ **Architettura: tienila proporzionata.** Puoi fare una struttura a livelli pulita
(Domain / Application / Infrastructure / Api) ma **non ripetere l'over-engineering di
LogiFlow**. A colloquio Marco ti chiederebbe subito "non è overkill?". La star è il
motore di settlement, non la cerimonia. Niente CQRS pesante se non serve.

**Tocco Oracle "vero"** (da citare a colloquio, mostra che non l'hai solo installato):
- usa **`MERGE`** per l'upsert idempotente delle righe di settlement, oppure
- una **vista materializzata** per il report mensile, oppure
- il lock di §6 con la sintassi Oracle. Basta uno di questi per dire "ho toccato
  qualcosa di specifico di Oracle, non solo EF Core sopra".

---

## 6. Il pezzo multi-server (chiude l'altra domanda del colloquio)

Il settlement gira come **job in background**. Domanda che ti hanno già fatto: *e se
girano due istanze del worker?* Qui lo **risolvi davvero** invece di raccontarlo.

- Ogni worker, prima di elaborare, **reclama** un run in stato Pending con un lock che
  salta le righe già prese:
  `SELECT ... FOR UPDATE SKIP LOCKED` — **che Oracle supporta nativamente**.
- Così due worker prendono run diversi e lavorano in parallelo, e lo stesso periodo non
  viene mai pagato due volte.
- Aggiungi un **timeout di reclamo** per recuperare i run lasciati appesi da un worker
  morto.

Quando a un prossimo colloquio ti chiedono di applicazioni multi-server, non dici più
"ho capito il concetto": dici *"nel mio progetto CER il settlement è un worker che può
girare su più istanze, e le coordino con SELECT FOR UPDATE SKIP LOCKED su Oracle, con
un timeout di reclamo per i run orfani."* Fine del buco.

---

## 7. Fasi di costruzione (ordine consigliato)

- **Fase 0 — Scheletro.** Solution .NET 8, Oracle XE in Docker, EF Core connesso, prima
  migration. Obiettivo: `docker compose up` e l'API risponde su un health check.
- **Fase 1 — Dominio + dati.** Le 6 entità, e un **generatore di letture orarie** (una
  settimana, qualche POD di produzione e consumo). Riusa l'idea del simulatore di Motor
  Valley Sentinel. In alternativa import da CSV.
- **Fase 2 — Il motore.** Calcolo condivisa oraria + incentivo + riparto. **Tutto unit
  tested** prima di attaccarlo al database. Questa è la fase che conta.
- **Fase 3 — Persistenza + idempotenza.** SettlementRun, MemberSettlement, HourlyShare;
  ri-esecuzione sicura; endpoint per l'estratto conto di un membro.
- **Fase 4 — Multi-worker.** Il background worker con il claim + SKIP LOCKED (§6).
- **Fase 5 (stretch).** Export report CSV in stile GSE; mini dashboard React.

Se ti fermi alla Fase 3 hai già un progetto da CV solido. Le Fasi 4–5 sono ciò che lo
rende *specificamente* la risposta a Teoresi.

---

## 8. Cosa dimostra (il framing per il CV e il colloquio)

- **Dominio Energy reale**: settlement, serie storiche, calcolo in stile regolatorio.
- **Lo stack esatto di Teoresi**: .NET 8, C#, **Oracle**, Docker, CI.
- **I due buchi chiusi**: Oracle con le mani, coordinamento multi-server con un problema
  vero.
- **Mentalità da sistema che muove denaro**: idempotenza, casi limite testati,
  correttezza prima di tutto — che è esattamente ciò che un revisore senior cerca.

**Bozza di voce per il CV** (stile coerente con gli altri tuoi progetti):

> **SolarLedger — Piattaforma di Settlement per Comunità Energetiche**
> *C#, .NET 8, Oracle, EF Core, Docker, xUnit*
> Motore di settlement per l'autoconsumo condiviso di una CER: ingestione di letture
> orarie per POD, calcolo dell'energia condivisa oraria come minimo tra immissione e
> prelievo della comunità, attribuzione dell'incentivo ai membri secondo policy di
> riparto sostituibili, ed esecuzione idempotente del settlement con coordinamento
> multi-worker (Oracle `SELECT ... FOR UPDATE SKIP LOCKED`) per l'esecuzione una-sola-
> volta per periodo. Persistenza su Oracle XE, logica di riparto coperta da unit test e
> percorsi su database verificati con Testcontainers.

Quella riga, da sola, risponde a tutto ciò che Teoresi ha sondato mercoledì.

---

## 9. Rischi / cose da non fare

- **Non trasformarlo in un prodotto reale conforme al GSE.** È un portfolio project.
  Dichiara le semplificazioni nel README — dirle è un segno di maturità, non un difetto.
- **Non gold-plare l'architettura.** Il valore è il motore + Oracle + multi-worker.
- **Non inventare i numeri delle tariffe** come se fossero ufficiali.
- **.NET 8, non 10.** Match con l'annuncio.

> Il primo commit dovrebbe già avere Oracle in Docker che parte: è la prova che volevi.
