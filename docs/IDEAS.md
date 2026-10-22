# Pomysły na rozwój

Pomysły, które mogą wejść do projektu w kolejnych etapach. Nie są jeszcze częścią zakresu z [wizji](VISION.md).

## Ocena zawodnika na podstawie profilu na Codeforces

Agent dostaje nick z Codeforces i opisuje, na jakim poziomie jest zawodnik i w czym jest mocny,
a w czym słaby. Sam rating tego nie mówi: dwie osoby z ratingiem 1300 mogą mieć zupełnie
inne umiejętności.

Przykład z danych, które już pobraliśmy: szeldon555 ma rating 1216 (maksymalnie 1477). Rozwiązał 32 zadania,
w tym 18 z matematyki i 12 z implementacji. Za to binary search i two pointers to po 2–3 rozwiązane zadania
przy około 35% zaakceptowanych zgłoszeń. Agent mógłby z tego napisać: „mocny w matematyce i zadaniach
konstrukcyjnych, słaby w binary search i two pointers, zadania do 1300 rozwiązuje pewnie, powyżej rzadko”.

Skąd dane:

- `user.info` — aktualny i maksymalny rating, ranga,
- `user.rating` — historia ratingu, czyli trend (rośnie, stoi, spada) i aktywność w konkursach,
- `user.status` — wszystkie zgłoszenia: rozwiązane zadania z tagami i ratingiem, odsetek zaakceptowanych
  zgłoszeń w każdym temacie, najtrudniejsze rozwiązane zadanie w każdym temacie.

Co agent zwraca: opis poziomu, listę mocnych i słabych tematów z liczbami, które to uzasadniają,
oraz propozycję, co ćwiczyć dalej.

Do czego się przyda w projekcie:

- **solo training:** słabe tematy z profilu to gotowe wejście do doboru kolejnego zadania,
- **contest:** profile wszystkich uczestników pozwalają ocenić poziom grupy dokładniej niż sam rating,
- **porównanie zawodników** przy lokalnych eliminacjach,
- **dobór drużyn** (pomysł niżej) — ocena profilu jest pierwszym krokiem.

Większość liczb da się policzyć w kodzie (narzędzie wołające API Codeforces). Model jest potrzebny
do interpretacji i opisu: co z tych liczb wynika i co zawodnik powinien robić dalej.

Pytania otwarte:

- od ilu rozwiązanych zadań w temacie ocena jest wiarygodna,
- jak traktować stare zgłoszenia (czy umiejętność sprzed dwóch lat nadal się liczy),
- czy oceniać po tagach Codeforces, które bywają niedokładne, czy np. po zarysach zadań z RAG.

## Dobór drużyn, które się uzupełniają

Agent porównuje zawodników na podstawie ich profili na Codeforces i układa z nich drużyny,
na przykład ICPC po 3 osoby.

Przykład: koło ma 10 osób. Agent dobiera drużyny tak, żeby zawodnicy mieli podobny rating,
ale różne mocne strony. W jednej drużynie jedna osoba dobrze zna dp, druga geometrię,
a trzecia grafy. Razem pokrywają więcej tematów niż trzy osoby o tym samym profilu.

Skąd dane:

- `user.info` — rating i ranga,
- `user.status` — rozwiązane zadania i ich tagi, z czego da się policzyć mocne i słabe tematy
  (liczba rozwiązanych zadań w temacie, odsetek zaakceptowanych zgłoszeń, rating zadań).

Co agent zwraca: podział na drużyny z uzasadnieniem, kto w drużynie odpowiada za które tematy
i jakich tematów drużynie nadal brakuje.

Pasuje do istniejącego działania contest: przy lokalnych eliminacjach wynik gymu
plus profile mówią, kto jest na jakim poziomie, a dobór drużyn jest naturalnym następnym krokiem.

Pytania otwarte:

- jak ważyć podobny rating względem uzupełniających się tematów,
- co z zawodnikami z małą liczbą rozwiązanych zadań, dla których profil tematów jest niewiarygodny,
- czy tagi Codeforces wystarczą do oceny umiejętności, czy potrzebna jest też historia gymów.
