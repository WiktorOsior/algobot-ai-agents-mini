# TrAIningRound

**Autorzy:**
- Jan Kaszycki
- Wiktor Osior

**TrAIningRound** to agent AI, który pomaga ćwiczyć zadania algorytmiczne, na przykład pod konkursy w stylu Codeforces.

Dobiera zadania z istniejących baz według poziomu użytkownika oraz tematów, w których jest mocny albo słaby. Pozwala układać contesty o zadanym rankingu albo na poziomie wskazanego konkursu, a także sprawdzać aktualny poziom rozwiązującego (rozwiązujących) w celu np. robienia lokalnych eliminacji.

## Stan projektu

| Etap | Stan |
|---|---|
| 1. Wizja projektu | gotowa: [docs/VISION.md](docs/VISION.md) |
| 2. Agent i model | w toku: wybór modelu zrobiony na podstawie eksperymentów z Laboratorium 03, agent jeszcze nie powstał |
| 3. Tool calling | do zrobienia |
| 4. MCP | do zrobienia |
| 5. RAG | do zrobienia |
| 6. Integracja | do zrobienia |
| 7. Ewaluacja | do zrobienia |

### Wybór modeli

Na podstawie [eksperymentów](experiments/lab03-models-and-prompting/reports/report.md):

- **odczyt intencji na czacie** (czy użytkownik chce zadanie dla siebie, czy contest, na jakim poziomie i z jakiego tematu):
  qwen2.5-coder:3b lokalnie w Ollamie, prompt zero-shot, temperatura 0, JSON Schema — 84% poprawnych odpowiedzi w niecałą sekundę,
- **układanie contestów dla grupy:** mocniejszy model, np. gemma-4-31b na serwerze w sieci lokalnej — dobry contest
  we wszystkich ocenianych scenariuszach, małe modele 3–4B sobie z tym nie radzą.

## Dokumentacja

- [Wizja](docs/VISION.md) — problem, użytkownicy, działania agenta i zakres
- [Pomysły na rozwój](docs/IDEAS.md) — ocena zawodnika na podstawie profilu, dobór drużyn
- Laboratorium 03 — modele i prompting:
  - [Raport z wnioskami](experiments/lab03-models-and-prompting/reports/report.md)
  - [Dokładny opis eksperymentów](experiments/lab03-models-and-prompting/reports/opis-eksperymentow.md) (dane, prompty, pełne interakcje z modelami)
  - [Układanie contestów — każdy contest osobno](experiments/lab03-models-and-prompting/reports/exp05-contesty.md)

## Struktura

```text
TrAIningRound/
├── README.md
├── docs/
│   ├── VISION.md                  # wizja projektu
│   └── IDEAS.md                   # pomysły na rozwój
├── experiments/
│   └── lab03-models-and-prompting/
│       ├── README.md              # opis eksperymentów i uruchomienie
│       ├── data/                  # wiadomości testowe, scenariusze contestów
│       ├── src/                   # program testowy ModelLab (C#, .NET 9)
│       ├── results/               # wyniki i logi
│       └── reports/               # raport, opis eksperymentów, zdjęcia
├── scripts/
│   └── fetch_codeforces_samples.py  # pobiera przykładowe zadania z Codeforces
└── data/                          # pobrane treści zadań (poza repozytorium)
```

## Uruchomienie

Agent jeszcze nie powstał. Na razie można uruchomić eksperymenty z wyboru modelu.

Wymagania: [.NET 9 SDK](https://dotnet.microsoft.com/download) i [Ollama](https://ollama.com/download) z modelami, np.:

```bash
ollama pull qwen2.5-coder:3b
```

Uruchomienie porównania modeli (z katalogu `experiments/lab03-models-and-prompting`):

```bash
dotnet run --project src/ModelLab -- EXP-01
```

Pełna lista eksperymentów, modeli i opcji (także model zdalny przez API zgodne z OpenAI):
[experiments/lab03-models-and-prompting/README.md](experiments/lab03-models-and-prompting/README.md).

### Przykładowe zadania z Codeforces

Treści zadań należą do Codeforces i ich autorów, dlatego nie ma ich w repozytorium (`data/codeforces/` jest w `.gitignore`).
Można je pobrać skryptem (liczba zadań, minimalny i maksymalny rating):

```bash
python3 scripts/fetch_codeforces_samples.py 10 800 1200
```

## Autorzy

**Jan Kaszycki**  
**Wiktor Osior**

© Jan Kaszycki, Wiktor Osior, 2026

## Licencja

Kod źródłowy jest udostępniany na licencji MIT.

Materiały pochodzące z zewnętrznych źródeł podlegają licencjom i prawom ich odpowiednich autorów.

Układ README i wizji wzorowany na projekcie [campus-ai-assistant](https://github.com/sulmar/campus-ai-assistant/), mgr Marcin Sulecki, Politechnika Warszawska, licencja CC BY 4.0.

Program testowy w `experiments/lab03-models-and-prompting` bazuje na kodzie `ModelLab` z repozytorium kursu
[pw-mini-ai-agents-dotnet](https://github.com/sulmar/pw-mini-ai-agents-dotnet), mgr Marcin Sulecki, licencja MIT.
