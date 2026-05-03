#!/usr/bin/env python3
import json
import os
import re
import sys
import urllib.error
import urllib.request
from datetime import datetime, timezone

COMMENT_RE = re.compile(r"<!--.*?-->", re.DOTALL)
HEADER_LINE_RE = re.compile(r"^\s*(?::cl:|🆑)\s*(.*)$", re.I)
ENTRY_RE = re.compile(
    r"^\s*[*-]?\s*(add|remove|tweak|fix|upstream|sponsor)\s*:\s*(.+)$",
    re.I,
)

EMOJI = {
    "add": ":new:",
    "remove": ":no_entry_sign:",
    "tweak": ":hammer_pick:",
    "fix": ":bug:",
    "upstream": "⚡",
}

ROLE_PING_DEFAULT = "1500334396283162684"
MONETKS_EMOJI_ID_DEFAULT = "1492181985420902610"
EMBED_COLOR = 0xFFF183


def monetks_emoji() -> str:
    eid = (os.environ.get("DISCORD_MONETKS_EMOJI_ID") or MONETKS_EMOJI_ID_DEFAULT).strip()
    return f"<:monetks:{eid}>"


def entry_icon(typ: str) -> str:
    if typ == "sponsor":
        return monetks_emoji()
    return EMOJI.get(typ, ":white_check_mark:")


def parse_body(body: str):
    body = COMMENT_RE.sub("", body or "")
    lines = body.splitlines()
    start = None
    tail = ""
    for i, line in enumerate(lines):
        m = HEADER_LINE_RE.match(line)
        if m:
            start = i
            tail = (m.group(1) or "").strip()
            break
    if start is None:
        return None, []

    author = tail
    entries = []
    i = start + 1
    while i < len(lines):
        raw = lines[i]
        s = raw.strip()
        if not s:
            i += 1
            continue
        em = ENTRY_RE.match(raw)
        if em:
            entries.append((em.group(1).lower(), em.group(2).strip()))
            i += 1
            continue
        if not author and not entries and not re.match(r"^\s*[-*]", raw):
            author = s
            i += 1
            continue
        break

    return author, entries


def build_embed_description(entries: list) -> str:
    lines = []
    for typ, text in entries:
        icon = entry_icon(typ)
        lines.append(f"{icon} {text}")
    return "\n".join(lines)


def build_payload(date_str: str, pr_url: str, author: str, entries: list, role_id: str) -> dict:
    title = f"— {date_str} {monetks_emoji()} MiniStation (by {author})"
    desc = build_embed_description(entries)
    return {
        "content": f"<@&{role_id}>",
        "allowed_mentions": {"roles": [role_id]},
        "embeds": [
            {
                "title": title[:256],
                "url": pr_url,
                "description": desc[:4096],
                "color": EMBED_COLOR,
            }
        ],
    }


def main():
    body = os.environ.get("PR_BODY", "")
    pr_author = os.environ.get("PR_AUTHOR", "unknown")
    pr_url = os.environ.get("PR_HTML_URL", "")
    webhook = (os.environ.get("CHANNEL_WEBHOOK_URL") or "").strip()

    author, entries = parse_body(body)
    if author is None or not entries:
        print("No :cl: changelog entries in PR body; skip webhook.")
        return

    if not webhook:
        print("::error::Set repository secret CHANNEL_WEBHOOK_URL", file=sys.stderr)
        sys.exit(1)

    display_author = author.strip() if author.strip() else pr_author
    role_id = (os.environ.get("NOTIFY_ROLE_ID") or "").strip() or ROLE_PING_DEFAULT
    now = datetime.now(timezone.utc)
    date_str = now.strftime("%d/%m/%y")
    payload_obj = build_payload(date_str, pr_url, display_author, entries, role_id)

    payload = json.dumps(payload_obj).encode("utf-8")
    repo = os.environ.get("GITHUB_REPOSITORY", "github.com")
    ua = f"MiniStation-ChangelogNotify/1.0 (+https://github.com/{repo})"
    req = urllib.request.Request(
        webhook,
        data=payload,
        headers={"Content-Type": "application/json", "User-Agent": ua},
        method="POST",
    )
    try:
        urllib.request.urlopen(req, timeout=30)
    except urllib.error.HTTPError as e:
        print(e.read().decode("utf-8", errors="replace"), file=sys.stderr)
        raise


if __name__ == "__main__":
    main()
