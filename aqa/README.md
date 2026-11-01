# Домашнє завдання Test Automation #8 — груповий pre-order

**Напрям:** 1 (груповий pre-order), варіант «лише Backend розгорнутий». Мій Backend із ДЗ (рівні 1 і 3) розгорнуто у власному AWS. Frontend із ДЗ не інтегровано з API (він працював на мок-бекенді), тому:
- усі сценарії рівнів **1** і **3** перевіряються **через API**;
- є **один мінімальний UI-тест** (вхід клієнта на вітрині), він іде на мій розгорнутий оригінальний фронтенд.

**Підхід:** BDD + Screenplay, як у наявному фреймворку (Reqnroll + TUnit + Playwright for .NET). Шари такі:
- `Features/` — Gherkin бізнес-мовою;
- `StepDefinitions/` — тонкі кроки;
- `Screenplay/Tasks` і `Screenplay/Questions` — дії та запитання актора;
- `Api/` — клієнти й DTO;
- `Ui/` — page objects.

**Рівень 2** (підтвердження консолідованого pre-order) не автоматизовано: у моєму Backend його немає, ендпоінта submit групи не існує.

## Що покрито

| Фіча | Рівень | Сценарії |
|---|---|---|
| `Features/GroupPreOrderParticipants.feature` | 1 | Owner бачить усіх гостей, їхні страви, порції та суму кожного. Загальна сума дорівнює сумі по учасниках і змінюється, коли гість додає або прибирає порцію. Непідтверджений кошик має відмітку «still choosing». Гість без страв у списку з нулем. Порожня група дає `participants: []`, `total: 0`. Список **не** бачать інший клієнт, гість групи, офіціант і неавторизований відвідувач. Неіснуюча група. |
| `Features/GroupPreOrderRemoval.feature` | 3 | Видалення одного з кількох: решта лишається, сума перерахована. Видалення гостя, що вже підтвердив кошик. Видалення останнього. Гість іншої групи (дві незалежні групи) дає відмову. Неіснуючий гість дає «not found». Повторне видалення. Видалити не може ніхто, крім Owner. |
| `Features/StorefrontSignIn.feature` | UI | Зареєстрований клієнт входить на вітрині. З неправильним паролем лишається на сторінці входу. |

Позитивні й негативні сценарії є на кожному рівні.

## Поведінка, яку ДЗ не визначає (обрана та зафіксована)

| Випадок | Очікування в тестах |
|---|---|
| Список або видалення без входу, або з токеном сесії гостя замість токена клієнта | `401` (Cognito-авторизатор не пропускає) |
| Інший клієнт або офіціант (не Owner цієї броні) | `403`, у відповіді немає даних гостей |
| Група не існує | `404` «Group reservation was not found» |
| Гість іншої групи | `400` «Guest session does not belong to this group reservation» |
| Гість не існує або вже видалений | `404` «Guest session … not found» |
| Видалення останнього гостя | Дозволено, далі `participants: []`, `total: 0` |
| Owner у списку | Не показується: у моїй реалізації список містить лише гостей |

Коди звірено з кодом Backend і перевірено на розгорнутому API.

## Тестові дані

- Кожен сценарій **сам створює** дані через API:
  - реєструє нового клієнта з унікальним email (`aqa-<guid>@example.com`);
  - бронює стіл на випадковий день через 2–6 місяців (вільний слот береться з API);
  - відкриває запрошення, гості приєднуються й додають страви з актуального меню.
- Очікувані суми рахуються з того, що гості поклали в кошик. У feature-файлах цифр немає.
- Після сценарію бронювання **скасовується** (`CleanupScope`). Видалити користувача API не дозволяє, тому використовуються унікальні email.
- Сценарії незалежні й ідуть паралельно (до 4).

## Налаштування

```bash
cd aqa/restaurant-aqa-framework
cp .env.example .env
```

У `.env` заповнити:

| Змінна | Значення |
|---|---|
| `UI_URL` | адреса свого фронтенду (CloudFront) |
| `API_URL` | адреса свого API разом зі stage, напр. `https://xxxx.execute-api.us-west-2.amazonaws.com/develop` |
| `WAITER_USERNAME`, `WAITER_PASSWORD` | *необов'язково*. Без них приклади з офіціантом (`@requires-waiter`) пропускаються |
| `TESTTABLE_ID` | *необов'язково*: конкретний стіл на 4+ місця. Без нього стіл знаходиться автоматично |

`.env` у `.gitignore`, його не комітимо.

## Запуск

```bash
dotnet build
dotnet run --no-build -- --report-trx --treenode-filter "/*/*/*/*[Category=hw8]"          # усе ДЗ
dotnet run --no-build -- --treenode-filter "/*/*/*/*[Category=level-1]"                   # рівень 1
dotnet run --no-build -- --treenode-filter "/*/*/*/*[Category=level-3]"                   # рівень 3
dotnet run --no-build -- --treenode-filter "/*/*/*/*[Category=ui-smoke]"                  # UI-тест
dotnet run --no-build -- --treenode-filter "/*/*/*/*[Category=negative]"                  # лише негативні
```

## Звіти

Усе лежить у `aqa/restaurant-aqa-framework/TestResults/`:
- TUnit HTML і TRX;
- Reqnroll HTML-звіт у `reqnroll/`;
- Playwright traces для впалих UI-сценаріїв у `traces/` (відкрити: `npx playwright show-trace <файл>.zip`).

## CI

Workflow `.github/workflows/aqa-tests.yml` запускається при push в `aqa/**`, вручну (Actions → AQA Tests → Run workflow, можна задати фільтр) і щоночі. Що він робить:
1. збирає проєкт;
2. ставить Chromium;
3. проганяє сценарії (впалий тест **не зупиняє** пайплайн, `continue-on-error`);
4. публікує звіт «AQA scenarios» і артефакт `aqa-test-results` (звіти й traces).

Налаштування в Settings → Secrets and variables → Actions:
- **Variables:** `AQA_UI_URL`, `AQA_API_URL`, за бажанням `AQA_TESTTABLE_ID`;
- **Secrets** (за бажанням): `AQA_WAITER_USERNAME`, `AQA_WAITER_PASSWORD`.

## Використання AI

Використовувався Claude (Anthropic): він розібрав фреймворк і контракт API, перевірив коди відповідей на розгорнутому API, згенерував шари (API-клієнт, tasks і questions, кроки, фічі) і CI. Я розібрав сценарії й Screenplay-структуру, налаштував змінні та запустив тести у себе й у CI.
