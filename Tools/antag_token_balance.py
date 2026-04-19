#!/usr/bin/env python3
# SPDX-FileCopyrightText: 2026 Mr_Samuel
#
# SPDX-License-Identifier: MIT

"""
Изменяет баланс антаг-монет (token_id = balance) в PostgreSQL для SS14.
Соответствует таблице player_antag_token и AntagTokenCatalog.BalanceEntryId.

Примеры:
  python Tools/antag_token_balance.py --connection-string "Host=localhost;Port=5432;Database=ss14;Username=ss14_user;Password=secret" --user SomePlayer --add 10
  python Tools/antag_token_balance.py --connection-string "..." --user 00000000-0000-0000-0000-000000000001 --delta -5
  python Tools/antag_token_balance.py --connection-string "..." --user SomePlayer --set 100

Требуется: pip install psycopg2-binary
"""

from __future__ import annotations

import argparse
import sys
from uuid import UUID

import psycopg2

BALANCE_TOKEN_ID = "balance"


def parse_args() -> argparse.Namespace:
    p = argparse.ArgumentParser(description="Изменить баланс antag tokens (balance) в БД.")
    p.add_argument(
        "--connection-string",
        required=True,
        help='Строка подключения PostgreSQL (как в dump_user_data.py / Npgsql).',
    )
    p.add_argument(
        "--user",
        required=True,
        help="UUID игрока (user_id) или last_seen_user_name из таблицы player.",
    )
    group = p.add_mutually_exclusive_group(required=True)
    group.add_argument("--add", type=int, metavar="N", help="Прибавить N монет.")
    group.add_argument("--sub", type=int, metavar="N", help="Вычесть N монет.")
    group.add_argument("--delta", type=int, metavar="N", help="Изменить баланс на N (может быть отрицательным).")
    group.add_argument("--set", type=int, metavar="N", dest="set_amount", help="Установить точный баланс N.")
    p.add_argument(
        "--dry-run",
        action="store_true",
        help="Показать результат без записи в БД.",
    )
    return p.parse_args()


def resolve_user_id(cur, name_or_uid: str) -> str:
    try:
        return str(UUID(name_or_uid))
    except ValueError:
        pass

    cur.execute(
        "SELECT user_id FROM player WHERE last_seen_user_name = %s ORDER BY last_seen_time DESC LIMIT 1",
        (name_or_uid,),
    )
    row = cur.fetchone()
    if row is None:
        print(f"Игрок '{name_or_uid}' не найден в player.", file=sys.stderr)
        sys.exit(1)
    return str(row[0])


def get_balance(cur, player_uid: str) -> int:
    cur.execute(
        "SELECT amount FROM player_antag_token WHERE player_id = %s AND token_id = %s",
        (player_uid, BALANCE_TOKEN_ID),
    )
    row = cur.fetchone()
    return int(row[0]) if row is not None else 0


def apply_balance_change(cur, player_uid: str, new_amount: int) -> None:
    if new_amount <= 0:
        cur.execute(
            "DELETE FROM player_antag_token WHERE player_id = %s AND token_id = %s",
            (player_uid, BALANCE_TOKEN_ID),
        )
        return

    cur.execute(
        """
        INSERT INTO player_antag_token (player_id, token_id, amount)
        VALUES (%s, %s, %s)
        ON CONFLICT (player_id, token_id) DO UPDATE SET amount = EXCLUDED.amount
        """,
        (player_uid, BALANCE_TOKEN_ID, new_amount),
    )


def main() -> None:
    args = parse_args()

    if args.add is not None:
        if args.add < 0:
            print("--add: ожидается неотрицательное число.", file=sys.stderr)
            sys.exit(2)
        delta = args.add
    elif args.sub is not None:
        if args.sub < 0:
            print("--sub: ожидается неотрицательное число.", file=sys.stderr)
            sys.exit(2)
        delta = -args.sub
    elif args.delta is not None:
        delta = args.delta
    else:
        delta = None

    conn = psycopg2.connect(args.connection_string)
    try:
        conn.autocommit = False
        cur = conn.cursor()
        player_uid = resolve_user_id(cur, args.user)

        cur.execute("SELECT 1 FROM player WHERE user_id = %s FOR UPDATE", (player_uid,))

        cur.execute(
            "SELECT amount FROM player_antag_token WHERE player_id = %s AND token_id = %s FOR UPDATE",
            (player_uid, BALANCE_TOKEN_ID),
        )
        row = cur.fetchone()
        current = int(row[0]) if row is not None else 0

        if args.set_amount is not None:
            if args.set_amount < 0:
                print("--set: отрицательный баланс недопустим.", file=sys.stderr)
                sys.exit(2)
            new_amount = args.set_amount
        else:
            new_amount = current + delta

        if new_amount < 0:
            print(
                f"Ошибка: баланс стал бы отрицательным (текущий {current}, изменение {delta}).",
                file=sys.stderr,
            )
            sys.exit(3)

        print(f"user_id={player_uid}")
        print(f"было: {current}")
        print(f"станет: {new_amount}")

        if args.dry_run:
            conn.rollback()
            print("(dry-run: изменения не сохранены)")
            return

        apply_balance_change(cur, player_uid, new_amount)
        conn.commit()
        print("Готово.")
    except Exception:
        conn.rollback()
        raise
    finally:
        conn.close()


if __name__ == "__main__":
    main()
