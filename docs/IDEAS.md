# Pomysły na rozwój

Pomysły, które mogą wejść do projektu w kolejnych etapach. Nie są jeszcze częścią zakresu z [wizji](VISION.md).

## Kierunek: trening drużyny, a nie tylko jednej osoby

Z programu będzie korzystało koło ICPC. ICPC to konkurs drużynowy (trzy osoby, jeden komputer),
więc najwięcej wartości ma pomoc **drużynie**: wspólne treningi, analiza wirtualnych contestów
i dobór składów. Solo training zostaje jako podstawa, bo z profili pojedynczych osób składa się
obraz całej drużyny.

Pomysły poniżej budują się jeden na drugim:

```text
profil zawodnika ──► trening drużyny we trójkę ──► dobór drużyn
       │
       └──► wiedza do słabych tematów (RAG)
```

Zasada we wszystkich: **liczby liczy kod, model je interpretuje.** Model nie zgaduje statystyk,
tylko wyjaśnia wyniki narzędzi i radzi, co z nimi zrobić.

## 1. Profil zawodnika z analizą contestów

Agent dostaje nick z Codeforces i opisuje, na jakim poziomie jest zawodnik, w czym jest mocny,
w czym słaby i jak radzi sobie na contestach. Sam rating tego nie mówi: dwie osoby z ratingiem
1300 mogą mieć zupełnie inne umiejętności.

Przykład z danych, które już pobraliśmy: szeldon555 ma rating 1216 (maksymalnie 1477). Rozwiązał 32 zadania,
w tym 18 z matematyki i 12 z implementacji. Za to binary search i two pointers to po 2–3 rozwiązane zadania
przy około 35% zaakceptowanych zgłoszeń.

Skąd dane:

- `user.info` — aktualny i maksymalny rating, ranga,
- `user.rating` — każda runda z ratingiem: miejsce, rating przed i po, czyli trend,
- `user.status` — wszystkie zgłoszenia: zadanie z tagami i ratingiem, werdykt, czas od startu
  i rodzaj udziału (`CONTESTANT` w trakcie rundy, `VIRTUAL`, `PRACTICE` czyli upsolve).

Co liczy kod:

- mocne i słabe tematy (rozwiązane zadania i odsetek zaakceptowanych zgłoszeń w każdym tagu),
- wyniki w ostatnich rundach: które litery rozwiązuje w trakcie, w jakim czasie, ile ma WA,
- co rozwiązuje dopiero po contescie (upsolve),
- trend ratingu i aktywność.

Co mówi model, na przykład: „A–B robisz pewnie w pierwsze 30 minut, C tylko w 3 z ostatnich 10 rund,
zwykle po kilku WA na zadaniach z math. Proponuję trening z math na 1400–1600”.

Pytania otwarte:

- od ilu rozwiązanych zadań w temacie ocena jest wiarygodna,
- jak traktować stare zgłoszenia (czy umiejętność sprzed dwóch lat nadal się liczy),
- czy wliczać zadania rozwiązane w drużynie — w zgłoszeniu drużynowym nie widać, kto je napisał.

## 2. Trening drużyny we trójkę

Agent dostaje trzy nicki (albo nazwę drużyny i jej skład) i pomaga drużynie trenować razem.

**Analiza wirtualnych contestów drużyny.** Zgłoszenia drużynowe widać w `user.status` każdego
członka (pole `author.teamName`, np. „PW TRYOUT”). Z nich kod liczy dla każdej wirtualki: ile zadań,
w jakim czasie, ile WA, które zadania zostały nierozwiązane i z jakich tematów. Model szuka wzorców,
np. „w 4 z 5 ostatnich wirtualek nie zrobiliście zadania z geometrią”.

**Wkład członków bierzemy z ich indywidualnych profili**, a nie ze zgadywania. Jeśli drużyna stale
nie robi geometrii, a solo nikt z trzech osób nie rozwiązał geometrii powyżej 1400, wniosek jest
uczciwy: drużynie brakuje kogoś od geometrii albo wspólnego treningu z tego tematu.

**Trening dla drużyny.** Zestaw zadań, których nie rozwiązał nikt z trójki, z naciskiem na
słabe tematy drużyny (tematy, w których nawet najlepszy z trójki jest słaby). Wynik to lista
do założenia mashupa na Codeforces.

## 3. Dobór drużyn, które się uzupełniają

Agent porównuje zawodników koła i układa z nich drużyny po 3 osoby.

Przykład: koło ma 12 osób. Agent dobiera drużyny tak, żeby zawodnicy mieli podobny rating,
ale różne mocne strony. W jednej drużynie jedna osoba dobrze zna dp, druga geometrię,
a trzecia grafy. Razem pokrywają więcej tematów niż trzy osoby o tym samym profilu.

Jak to policzyć (kod, nie model):

- dla każdego zawodnika siła w każdym głównym temacie (z profilu z punktu 1),
- przy kole liczącym ok. 15 osób można sprawdzić wszystkie trójki (455 możliwości),
- ocena trójki: pokrycie tematów (najlepszy członek w każdym temacie) i wyrównany rating,
- kod zwraca najlepsze podziały, a model wyjaśnia, kto za co odpowiada i czego drużynie brakuje.

Model może też odpowiadać na pytania typu „a co, jeśli zamienię Bartka z Darkiem?” — przez
ponowne wywołanie narzędzia z innym składem, a nie przez zgadywanie.

Pytania otwarte:

- jak ważyć podobny rating względem uzupełniających się tematów,
- co z zawodnikami z małą liczbą rozwiązanych zadań, dla których profil tematów jest niewiarygodny,
- czy uwzględniać wyniki wspólnych wirtualek, jeśli dana trójka już razem grała.

## 4. Wiedza do słabych tematów (RAG)

Baza wiedzy z artykułów o algorytmach, np. [cp-algorithms](https://cp-algorithms.com)
(licencja CC BY-SA), podzielona na fragmenty i zindeksowana embeddingami.

Gdy profil pokaże słaby temat, agent wyszukuje pasujący fragment, przekazuje go modelowi jako
kontekst i odpowiada na jego podstawie, z linkiem do źródła:

> Najsłabiej idzie Ci binary search (2/9). Kluczowa idea: szukanie binarne po wyniku, gdy
> sprawdzenie „czy da się osiągnąć X” jest monotoniczne *(cp-algorithms, link)*.
> Spróbuj tych dwóch zadań: ...

Agent wskazuje materiał i zadania, ale nie pomaga w konkretnym zadaniu w trakcie rozwiązywania
(to byłby „teacher” z wizji, który jest poza zakresem).

## Eksperyment: czy podobna treść znaczy podobne zadanie?

Pierwotny pomysł na RAG to wyszukiwanie zadań po opisie treści („coś o szachownicy”,
„podobne do 1512D”). Wątpliwość: embedding treści łapie głównie fabułę zadania, a nie technikę
rozwiązania. Dwa zadania o robotach mogą być „podobne”, choć jedno to greedy, a drugie dp.

Zamiast od razu budować na tym funkcję, można to sprawdzić: na kilkuset zadaniach porównać
podobieństwo embeddingów treści z pokryciem tagów. Jeśli korelacja jest słaba, wyszukiwanie
po treści nie ma sensu dla treningu — i to też jest dobry, udokumentowany wynik.

## Odrzucone pomysły

**Rozpoznawanie autora kodu w drużynie po stylu.** API Codeforces nie zwraca kodu, a zgłoszenie
drużynowe nie mówi, który członek je wysłał. Kod trzeba by scrapować z logowaniem na konto,
co jest kruche i niezgodne z zasadami korzystania z serwisu przez automaty. Przy trzech osobach
z podobnym stylem (ten sam szablon, C++) zgadywanie byłoby mało trafne, a wnioski o konkretnych
ludziach („to pewnie on zawalił C”) — zawodne i niezręczne. Wkład członków oceniamy z ich
indywidualnych profili (punkt 2).

**Zakładanie gymów przez API.** API Codeforces nie pozwala tworzyć contestów. Agent przygotowuje
listę zadań, a mashup zakłada się ręcznie.

**Automatyczny poziom konkursów z gyma (np. AMPPZ, CERC).** Zadania z gyma nie mają ratingu,
wyniki gyma (`contest.standings`) wymagają logowania kluczem API, a AMPPZ nie ma w publicznej
liście gymów. Zamiast tego można ręcznie zapisać profil trudności konkursu
(np. „AMPPZ ≈ 10 zadań, rating 1200–2600”) na podstawie doświadczenia koła.
Dla zwykłych rund Codeforces poziom da się odczytać wprost, bo ich zadania mają rating.
