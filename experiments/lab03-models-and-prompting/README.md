# Laboratorium 03 — Modele i prompting na zadaniu TrAIningRound

Eksperymenty z [Laboratorium 03](https://github.com/sulmar/pw-mini-ai-agents-dotnet/tree/main/docs/labs/03-models-and-prompting)
wykonane na zadaniu z naszego projektu zamiast notatek kierowców.

- **Raport z wynikami i wnioskami: [reports/report.md](reports/report.md)**
- **Dokładny opis eksperymentów** (dane, prompty, wyniki każdej wiadomości, pełne interakcje):
  [reports/opis-eksperymentow.md](reports/opis-eksperymentow.md)
- **Układanie contestów, każdy contest osobno:** [reports/exp05-contesty.md](reports/exp05-contesty.md)

## Zadania dla modelu

**EXP-01 – EXP-04: odczyt intencji z czatu.** Z wiadomości użytkownika model wyciąga:

```json
{ "mode": "solo", "rating_min": 1216, "rating_max": 1216, "topic": "binary search" }
```

- `mode` — `solo` (zadanie dla siebie), `contest` (contest, runda, eliminacje dla grupy) albo `null`,
- `rating_min`, `rating_max` — zakres trudności; jedna liczba trafia do obu pól, ranga albo dywizja
  zamienia się na zakres z tabeli progów, `null`, gdy nie da się ustalić,
- `topic` — tag Codeforces albo `null`.

Progi rang i dywizji są wpisane na sztywno w prompt ([Codeforces.cs](src/ModelLab/Codeforces.cs)).

**EXP-05: dobry contest dla grupy.** Model dostaje uczestników (rating i zadania, które już rozwiązali)
oraz pulę 20 zadań (id, rating, tagi) i wybiera 4 zadania z uzasadnieniem. Kod sprawdza:

| Kryterium | Warunek |
|---|---|
| poprawny | dokładnie 4 różne zadania z puli |
| nierozwiązane | żaden uczestnik nie rozwiązał wybranego zadania |
| start | najłatwiejsze zadanie od (najsłabszy − 500) do najsłabszego |
| koniec | najtrudniejsze zadanie od (najmocniejszy − 100) do (najmocniejszy + 300) |
| rozrzut | różnica ratingów najtrudniejszego i najłatwiejszego co najmniej 200 (kolejność nie ma znaczenia) |
| tematy | każde zadanie ma co najmniej jeden tag, którego nie mają pozostałe trzy |
| uzasadnienie | co najmniej 40 znaków |

Contest jest **dobry**, gdy spełnia wszystkie kryteria. Słowa „uczciwy” używamy tylko w węższym znaczeniu
kryterium „Nierozwiązane”: nikt z grupy nie zna wcześniej żadnego zadania, więc nikt nie ma przewagi.

Gdy w puli nie ma nierozwiązanego zadania poniżej najsłabszego (w okolicy najmocniejszego) uczestnika,
start (koniec) liczymy względem najbliższego dostępnego zadania.

Po zmianie kryteriów zapisane odpowiedzi można ocenić ponownie bez pytania modeli:

```bash
dotnet run --project src/ModelLab -- EXP-05-RESCORE
```

## Dane

| Plik | Zawartość |
|---|---|
| [data/messages.csv](data/messages.csv) | 25 wiadomości testowych z oczekiwanym wynikiem |
| [data/prompt-examples.csv](data/prompt-examples.csv) | 5 przykładów do one-shot i few-shot (inne niż testowe) |
| [data/contest-scenarios.json](data/contest-scenarios.json) | pula zadań (same metadane z API) i 5 grup uczestników |

Profile `szeldon555`, `bstopczynski` i `jkssss` to prawdziwe konta z Codeforces (rating z 21.10.2026).
Pozostali uczestnicy są wymyśleni, żeby sprawdzić, czy model omija zadania, które ktoś już rozwiązał.

## Uruchomienie

Wymagana Ollama z modelami:

```bash
ollama pull gemma3:4b
```

```bash
ollama pull qwen3:4b-instruct
```

```bash
ollama pull qwen2.5-coder:3b
```

```bash
ollama pull gemma4:e4b
```

Z katalogu `experiments/lab03-models-and-prompting`:

```bash
dotnet run --project src/ModelLab -- EXP-01
```

```bash
dotnet run --project src/ModelLab -- EXP-02 qwen2.5-coder:3b
```

Drugi argument to model. W EXP-01 i EXP-05 bez niego uruchamiane są wszystkie modele z listy.

Model na zdalnym serwerze zgodnym z API OpenAI (np. llama.cpp) podaje się z prefiksem `openai/`,
a adres serwera w zmiennej `OPENAI_BASE_URL` (klucz, jeśli potrzebny, w `OPENAI_API_KEY`):

```bash
OPENAI_BASE_URL=http://adres-serwera:port/v1 dotnet run --project src/ModelLab -- EXP-01 openai/gemma-4-31b:Q8_K_XL
```

Wyniki trafiają do `results/results.csv` (EXP-01 – EXP-04) i `results/contest-results.csv` (EXP-05),
a pełne odpowiedzi modeli do logów `results/EXP-XX.log`.

Kod bazuje na `ModelLab` z repozytorium kursu (mgr Marcin Sulecki, licencja MIT).
