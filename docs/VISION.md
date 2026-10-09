# TrAIningRound

Osoby przygotowujące się do konkursów algorytmicznych często mają trudności
z doborem zadań do ćwiczeń. Chcemy stworzyć program, który pomoże im
trenować na własnym poziomie i ułatwi układanie treningów.

Użytkownik może dostać zadania wybrane z istniejących baz według swojego
poziomu oraz tematów, w których jest mocny albo słaby, na przykład pod
konkursy w stylu Codeforces.

Może też ułożyć contest o zadanym rankingu albo na poziomie wskazanego
konkursu.

Program pozwala sprawdzić poziom rozwiązującego albo grupy rozwiązujących,
na przykład przy lokalnych eliminacjach.

## Dwa działania

Są dwa główne działania: solo training i contest.

Solo training to pojedyncze zadania do ćwiczenia słabych stron. Użytkownik
dostaje jedno zadanie naraz, dobrane do tematów, które wymagają pracy.

Contest to runda o zadanym rankingu albo na poziomie wskazanego konkursu.
Może być dla jednej osoby albo dla grupy, na przykład przy lokalnych
eliminacjach.

## Rozmowa

Użytkownik rozmawia z agentem na czacie. Tu ustala, które z dwóch działań
chce zacząć.

Agent doprecyzowuje poziom, tematy i to, czy contest jest dla jednej
osoby, czy dla grupy.

## Solo training

Agent od doboru zadań sięga po nie z istniejącej bazy, na przykład przez
API Codeforces, albo z własnego indeksu w stylu RAG.

Pamięta umiejętności użytkownika i zadania, które ten już zrobił, żeby
nie proponować ich ponownie. Kolejne zadanie trafia wtedy w słabą stronę,
a nie w temat już przećwiczony.

Przy rozwiązywaniu pomaga agent debugger (teacher). Tłumaczy treść zadania
i pomaga namierzyć błąd w kodzie.

## Contest

Agent od contestów układa rundę. Przez API Codeforces zakłada gym o zadanym
rankingu albo na poziomie wskazanego konkursu i zaprasza do niego graczy.

Przy lokalnych eliminacjach zaproszenie może objąć całą grupę. Wynik
gymu pomaga ocenić, kto na jakim jest poziomie.

## Zakres

W zakresie są dwa działania: solo training i contest.

Solo training obejmuje dobór jednego zadania na słabą stronę, z istniejącej
bazy, oraz pomoc przy jego rozwiązywaniu: tłumaczenie treści i namierzenie
błędu w kodzie. Program pamięta umiejętności użytkownika i zadania, które
ten już zrobił.

Contest obejmuje ułożenie gymu przez API Codeforces, o zadanym rankingu
albo na poziomie wskazanego konkursu, zaproszenie graczy i odczytanie
z wyniku, kto na jakim jest poziomie.

Do obu działań prowadzi czat, na którym użytkownik ustala, co chce zacząć.

## Poza zakresem

Projekt nie układa własnych zadań i nie prowadzi własnego sędziego.
Rozwiązania sprawdza Codeforces.

Contest w tym projekcie to gym, nie oficjalna runda oceniana na
Codeforces.

Teacher i debugger działają przy solo trainingu. Nie podpowiadają
w trakcie contestu i nie oddają gotowego rozwiązania za użytkownika.

Czat służy do ustalenia treningu albo contestu. Nie jest ogólnym
asystentem ani kursem algorytmiki.
