"""Pobiera kilka przykładowych zadań z Codeforces do data/codeforces/.

Treści zadań należą do Codeforces i ich autorów, dlatego katalog
data/codeforces/ jest w .gitignore. Do repozytorium trafia tylko ten skrypt.

Użycie:
    python3 scripts/fetch_codeforces_samples.py [liczba_zadań] [min_rating] [max_rating]

Kolejne uruchomienia dopisują zadania do index.json i pomijają już pobrane.
"""

import html
import json
import random
import re
import sys
import time
import urllib.request
from pathlib import Path

API_URL = "https://codeforces.com/api/problemset.problems"
PROBLEM_URL = "https://codeforces.com/problemset/problem/{contest}/{index}"
USER_AGENT = "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 Chrome/130 Safari/537.36"
OUT_DIR = Path(__file__).resolve().parent.parent / "data" / "codeforces"

# Kolejne tagi, z których bierzemy po jednym zadaniu. Zadania mają też inne
# tagi, więc greedy, math czy implementation powtarzają się między nimi.
TAG_ORDER = [
    "greedy", "math", "implementation", "strings", "sortings",
    "brute force", "constructive algorithms", "number theory", "dp",
    "two pointers", "games", "binary search",
]
REQUEST_DELAY_S = 2  # limit Codeforces: ok. 1 zapytanie na 2 s


def fetch(url):
    request = urllib.request.Request(url, headers={"User-Agent": USER_AGENT})
    with urllib.request.urlopen(request, timeout=30) as response:
        return response.read().decode("utf-8", errors="replace")


def pick_problems(problems, count, min_rating, max_rating, skip_ids):
    rng = random.Random(42)
    candidates = [
        p for p in problems
        if min_rating <= p.get("rating", 0) <= max_rating and 2 <= len(p["tags"]) <= 4
        and f"{p['contestId']}{p['index']}" not in skip_ids
    ]
    rng.shuffle(candidates)

    chosen, seen_tags = [], set()
    for tag in TAG_ORDER:
        if len(chosen) == count:
            break
        with_tag = [p for p in candidates if tag in p["tags"] and p not in chosen]
        # Preferujemy zadania, które dzielą jakiś tag z już wybranymi.
        with_tag.sort(key=lambda p: not (seen_tags & set(p["tags"])))
        if with_tag:
            chosen.append(with_tag[0])
            seen_tags.update(with_tag[0]["tags"])
    return chosen


def html_to_text(fragment):
    fragment = re.sub(r"<br\s*/?>", "\n", fragment)
    fragment = re.sub(r"</(p|div|li|pre)>", "\n", fragment)
    fragment = re.sub(r"<li>", "- ", fragment)
    text = html.unescape(re.sub(r"<[^>]+>", "", fragment))
    text = text.replace("$$$", "$")
    text = re.sub(r"[ \t]+", " ", text)
    return re.sub(r"\n\s*\n+", "\n\n", text).strip()


def parse_statement(page):
    match = re.search(r'<div class="problem-statement">(.*?)</div>\s*</div>\s*<script', page, re.S)
    if not match:
        raise ValueError("nie znaleziono treści zadania")
    body = match.group(1)

    def section(css_class):
        m = re.search(rf'<div class="{css_class}">(.*?)(?=<div class="(?:input-specification|output-specification|sample-tests|note)">|$)', body, re.S)
        return html_to_text(m.group(1)) if m else ""

    legend = re.search(r'</div></div><div>(.*?)</div><div class="input-specification">', body, re.S)
    samples = re.findall(r'<div class="(input|output)"><div class="title">.*?</div><pre[^>]*>(.*?)</pre>', body, re.S)

    return {
        "legend": html_to_text(legend.group(1)) if legend else "",
        "input": re.sub(r"^Input\s*", "", section("input-specification")),
        "output": re.sub(r"^Output\s*", "", section("output-specification")),
        "note": re.sub(r"^Note\s*", "", section("note")),
        "samples": [(kind, html_to_text(text)) for kind, text in samples],
    }


def to_markdown(problem, url, statement):
    lines = [
        f"# {problem['contestId']}{problem['index']} — {problem['name']}",
        "",
        f"- Link: {url}",
        f"- Rating: {problem['rating']}",
        f"- Tagi: {', '.join(problem['tags'])}",
        "",
        "## Treść", "", statement["legend"], "",
        "## Wejście", "", statement["input"], "",
        "## Wyjście", "", statement["output"], "",
    ]
    for kind, text in statement["samples"]:
        lines += [f"## Przykład — {'wejście' if kind == 'input' else 'wyjście'}", "", "```", text, "```", ""]
    if statement["note"]:
        lines += ["## Uwagi", "", statement["note"], ""]
    return "\n".join(lines)


def main():
    count = int(sys.argv[1]) if len(sys.argv) > 1 else 10
    min_rating = int(sys.argv[2]) if len(sys.argv) > 2 else 800
    max_rating = int(sys.argv[3]) if len(sys.argv) > 3 else 1200
    problems_dir = OUT_DIR / "problems"
    problems_dir.mkdir(parents=True, exist_ok=True)

    data = json.loads(fetch(API_URL))
    if data["status"] != "OK":
        sys.exit(f"Błąd API: {data.get('comment')}")

    index_path = OUT_DIR / "index.json"
    index = json.loads(index_path.read_text(encoding="utf-8")) if index_path.exists() else []
    known_ids = {entry["id"] for entry in index}
    added = 0

    for problem in pick_problems(data["result"]["problems"], count, min_rating, max_rating, known_ids):
        problem_id = f"{problem['contestId']}{problem['index']}"
        url = PROBLEM_URL.format(contest=problem["contestId"], index=problem["index"])
        time.sleep(REQUEST_DELAY_S)
        try:
            statement = parse_statement(fetch(url))
        except Exception as error:
            print(f"{problem_id}: pominięte ({error})")
            continue

        (problems_dir / f"{problem_id}.md").write_text(to_markdown(problem, url, statement), encoding="utf-8")
        index.append({
            "id": problem_id,
            "name": problem["name"],
            "url": url,
            "rating": problem["rating"],
            "tags": problem["tags"],
        })
        added += 1
        print(f"{problem_id} ({problem['rating']}) {', '.join(problem['tags'])}")

    index_path.write_text(json.dumps(index, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"\nDodano {added} zadań, razem {len(index)} w {OUT_DIR}")


if __name__ == "__main__":
    main()
