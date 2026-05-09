# SOSInsurance

SOSInsurance is a mod for [Delivery And Beyond](https://store.steampowered.com/app/3376480/Delivery__Beyond/) that "i" made.

The reason I said "i" made it is because this is the first mod I've tried making and I decided to use AI to help me since I had no idea what I was doing, but I ended up just using AI to make everything. So basically I didn't make this mod, AI did — but I did come up with the ideas for it and how all the systems should work.

The name came from thinking about what the mod actually does — it saves you from losing your scrap. That made me think of S.O.S, and it clicked: S.O.S normally stands for "Save Our Souls", but here it means **"Save Our Scrap"**. Felt like a perfect fit.

In vanilla, leaving a map means losing all your scrap. SOSInsurance changes that — dial **505** on the in-game phone to get connected to S.O.S Insurance, buy a policy, and when you leave a map you'll only lose a portion of your scrap instead of all of it.

---

## How It Works

Pick up the phone and dial **505**. You'll be connected to the S.O.S Insurance menu:

- **1 — Check Status:** Shows whether your policy is active and how many takeoffs you have left on it
- **2 — Buy/Renew:** Purchases insurance for $350 (or less if there's a sale on). Each purchase adds 7 takeoffs of coverage, and you can stack up to 21 total
- **3 — Quit:** Hangs up the call

When you go to buy, you'll be shown a confirmation screen with the cost and how many takeoffs you're getting before any money is taken. If you can't afford it, it'll tell you. If the call sits idle long enough it'll hang up on its own.

### Scrap Loss on Takeoff

When you leave a map with an active policy, one takeoff is consumed and your scrap is partially saved. How much you keep depends on how "worn" your policy is — the longer you've held it without renewing, the more scrap you lose per takeoff (5% more each takeoff by default, capped at 100% loss and with a minimum of 10% always kept). This means it's worth renewing regularly if you want to keep as much scrap as possible.

There's also an optional random loss mode where instead of scaling loss, a random percentage is rolled each time.

### Sales

Each session there's a 10% chance insurance goes on sale, dropping from $350 to $150. If a sale is active you'll see it on the menu, and a notification can be shown to announce it (configurable).

---

## Features

- Dial **505** on the in-game phone to access the insurance menu
- Buy coverage that protects your scrap when leaving a map
- **Takeoff-based coverage** — each policy lasts a set number of takeoffs (default 7), stackable up to a max (default 21)
- **Scaling loss system** — the longer you go without renewing, the more scrap you lose on a claim
- **Random loss mode** — optionally replace scaling loss with a random roll between a min and max percent
- **Minimum retention** — you always keep at least a percentage of your scrap no matter what (default 10%)
- **Random sales** — each session has a configurable chance of insurance going on sale at a reduced price
- **Sale announcements** — optionally broadcasts a notification to all players when a sale is active
- **Expiry warnings** — warns you when your coverage is running low
- **Inactivity timeout** — the call automatically ends if you leave the phone sitting idle
- **Confirmation screen** before any purchase so you don't accidentally spend your scrap
- **Fully configurable** — costs, durations, loss rates, sale chances, timeouts and more are all adjustable in the config file

---

## Config Options

| Setting | Default | Description |
|---|---|---|
| `SaleAnnouncement` | `true` | Show a message when insurance is on sale |
| `ExpiryWarningTakeoffs` | `2` | Warn when this many takeoffs remain |
| `InactivityTimeout` | `120` | Seconds before an idle call is hung up |
| `DebugLogging` | `false` | Enable verbose logging |
| `BaseCost` | `350` | Normal price of insurance |
| `SaleCost` | `150` | Sale price of insurance |
| `SaleChance` | `10` | % chance of a sale each session |
| `DurationTakeoffs` | `7` | Takeoffs added per purchase |
| `MaxStackedTakeoffs` | `21` | Max takeoffs you can stack |
| `DailyLossPercent` | `5` | % of scrap lost per takeoff used |
| `MaxLossPercent` | `100` | Max total loss % |
| `MinRetentionPercent` | `10` | Minimum % of scrap always kept |
| `UseRandomLoss` | `false` | Use random loss instead of scaling |
| `RandomLossMin` | `0` | Min % for random loss roll |
| `RandomLossMax` | `100` | Max % for random loss roll |

https://steamcommunity.com/sharedfiles/filedetails/?id=3722901866
