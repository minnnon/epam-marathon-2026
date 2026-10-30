# Домашнє завдання Frontend — Owner Review групового pre-order

Виконано **практичне завдання**, **рівень 1** і **рівень 2** (на мок-бекенді з архіву).
Angular 22, standalone-компоненти, `inject()`, `input()`, стан — `signal()` / `computed()`, без нових `Subject`.

Сторінка: `/reservations/demo-reservation/group-order?demo`

## Що зроблено

### Практичне + рівень 1 — верстка
- `components/group-orders/participant-card/` — картка учасника: заголовок «Guest: …» / «Owner: …», страви (фото 40×40 або перша літера на зеленому колі), кількість `× N`, ціна через `currency`, Subtotal, статус кошика **текстом** («Submitted» / «Still choosing»), а не лише кольором.
- `pages/group-order-page/` + маршрут у `app.routes.ts` (`reservations/:reservationId/group-order`, `AuthGuard`).
- Шапка: адреса, дата (`date: 'mediumDate'`), час (`date: 'HH:mm'`), «4 of 5 guests» — з іконками за макетом.
- Семантика: один `<main>`, один `<h1>`, учасники — `<ul>/<li>`, картка — `<article>`.
- Стилі в `.scss` поруч із компонентами, кольори з `app/shared/styles/abstracts/colors` через `@use … as color`.
- Ширина 375 px: без горизонтального скролу (перевірено в емуляції мобільного вікна).

### Рівень 2 — логіка на мок-бекенді
- `services/group-orders/group-orders.service.ts`:
  - `getGroupInvite$(reservationId)` — `invite-token` → `guest/invites/{token}` (`switchMap`);
  - `getSessions$(groupId)`, `getStatus$(groupId)`;
  - `getOwnerSessionId$(reservationId)` — повертає рядок `guestSessionId` (`map`);
  - `submit$(groupId, reservationId)` — `POST` з тілом `{ reservationId }`.
- Сторінка бере `reservationId` з URL, вантажить запрошення, далі паралельно (`forkJoin`) кошики, id сесії Owner і статус.
- Власна картка Owner — **My Pre-Order** (`isMe = input(false)` у картці).
- Блок **Consolidated pre-order** — `computed(() => consolidatePreOrder(participants()))`; однакові страви одним рядком («Spring Salad × 3»).
- **Grand Total = сума консолідованого замовлення** (лише підтверджені кошики), а не `total` з API.
- Жовта плашка «N participants haven’t submitted their cart yet — their dishes won’t be included in the order».
- Бейдж статусу: Open → «Collecting pre-orders», Submitted → «Sent to Restaurant».
- Стани: loading, error з «Try again», 404 → «Group reservation not found», empty → «Participants haven’t added any dishes yet».
- **Submit Pre-Order** → підтвердження через спільний `<app-modal>` (Cancel / Submit) → запит. Поки запит іде, кнопка «Submitting…» неактивна — подвійний клік не відправляє двічі. Кнопка неактивна, якщо в підтверджених учасників немає страв.
- Після успіху: toast «Pre-order sent to the restaurant», бейдж «Sent to Restaurant», екран read-only (кнопки Submit немає).
- Помилка submit: екран не ламається, кнопку можна натиснути знову; toast з помилкою вже показує `tokenInterceptor` — не дублюється.
- Підписки закриваються через `takeUntilDestroyed`.

## Перевірка

| Перевірка | Результат |
|---|---|
| `npx tsc -p tsconfig.app.json --noEmit` | без помилок |
| `npx tsc -p tsconfig.spec.json --noEmit` | без помилок |
| `BACKEND_URL=https://example.com npm run build` | `Bundle check passed: no configuration placeholders left.`; чанк `group-order-page-component` 14.76 kB; попередження про бюджети — наявний борг проєкту, не з цих файлів |
| `npm run test:hw` | **52 of 52 SUCCESS** (практичне + сервіс рівня 2 + сторінка рівня 2 + `shared/helpers`) |

Вручну на `?demo`: Grand Total $50.50 при сумі всіх кошиків $74.50 — $24 кошика Mariia («Still choosing») не потрапляють у замовлення; Submit → діалог → toast і read-only екран; неіснуюче бронювання → «Try again»; ширина 375 px.

## Скріни станів

| | |
|---|---|
| Завантаження | ![](screenshots/01-loading.jpg) |
| Owner Review | ![](screenshots/02-owner-review-top.jpg) |
| Діалог Submit | ![](screenshots/03-submit-dialog.jpg) |
| Sent to Restaurant | ![](screenshots/04-sent-to-restaurant.jpg) |
| Помилка + Try again | ![](screenshots/05-error-try-again.jpg) |
| Мобільний, 375 px | ![](screenshots/06-mobile-375.jpg) |

## Використання AI

Так, використовувався Claude (Anthropic) як ментор і помічник.
- Практичне і рівень 1 (шаблон шапки, підключення стилів, іконки) я писав сам за покроковими підказками; AI пояснював синтаксис (`{{ }}`, пайпи, сигнали), розбирав помилки компіляції і дав стартові SCSS-стилі.
- Сервіс і сторінку рівня 2 згенерував AI за вимогами з `homework-frontend.md` і тестами; я розібрав, що робить кожна частина (`switchMap`, `map`, `forkJoin`, `computed`).
- Результат перевірено чотирма перевірками з таблиці вище і вручну в браузері.

## Файли

```
src/app/app.routes.ts                                              маршрут сторінки
src/app/services/group-orders/group-orders.service.ts              сервіс
src/app/pages/group-order-page/group-order-page.component.*        сторінка
src/app/components/group-orders/participant-card/participant-card.component.*   картка
```
