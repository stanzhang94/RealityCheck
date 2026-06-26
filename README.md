# Reality Check

**Reality Check** is a financial pressure mod for *Stardew Valley*. It adds weekly taxes, health insurance, debt handling, and financial reports so your farm economy feels a little less consequence-free.

This mod is not designed to make the game easier. It is designed to make money feel like something that has to be managed.

## Features

### Weekly tax system

Reality Check adds a weekly tax settlement cycle. Taxes are assessed during the week and settled as a weekly obligation.

Current tax categories include:

- **Income Tax**  
  Applied to taxable shipping bin income. Direct shop sales are not taxed, leaving room for cash-style avoidance.

- **Property Tax**  
  Based on farm buildings, house upgrades, utility value, risk protection value, depreciation, agricultural deductions, and administrative fees.

- **Business Property Tax**  
  Applied to production equipment when a machine category exceeds the taxable threshold. If a category exceeds the threshold, the full count of that category is taxed.

### Custom weekly tax notice

After weekly tax settlement, the player receives a formal tax notice by mail.

The custom tax notice includes:

- Tax period and settlement date
- Income Tax details
- Property Tax assessment summary
- Business Property Tax formulas
- Total tax due
- Signature confirmation

The player must sign the notice before closing it with the close button. Esc remains available as a safety exit.

Historical tax review is handled through the Tax Report and financial reports. Old tax notices are not currently replayed from the vanilla mail history.

### Health insurance

Reality Check adds a Harvey medical insurance system.

Current behavior:

- Daily health insurance premium: **20g**
- Medical insurance coverage rate: **50%**
- Collapse or death-related medical expenses are detected from actual money loss
- Insurance reimbursement is processed the next morning
- Harvey Medical Clinic sends an insurance claim notice
- Reimbursement is recorded as an expense offset, not income

### Financial reports

Reality Check tracks and displays financial information through in-game reports, including:

- Daily report
- Seasonal report
- Annual report
- Tax report
- Tax history
- Income details
- Expense details
- Outstanding balance

The goal is not to replace every shop receipt in the game, but to give the player a clear picture of long-term financial pressure.

### Outstanding balance

If a Reality Check obligation cannot be paid immediately, the unpaid amount becomes outstanding balance.

Outstanding balance is not shown inside the Tax Notice itself. It belongs to the broader financial report system.

## Requirements

- Stardew Valley 1.6+
- SMAPI 4.0+

## Installation

1. Install SMAPI.
2. Download Reality Check.
3. Unzip the mod folder into your `Stardew Valley/Mods` folder.
4. Launch the game through SMAPI.

## Configuration

Reality Check includes a `config.json` generated after first launch.

Some parameters are configurable, including:

- Economic Report hotkey
- Tax notice mail toggle
- Tax notice signature requirement
- Income tax brackets
- Business property tax threshold
- Business property daily tax rates
- Property tax fee values

The default key to open the Economic Report is `O`. You can change it in `config.json`:

```json
"OpenReportKey": "O"
```

Examples:

```json
"OpenReportKey": "F8"
"OpenReportKey": "LeftAlt + P"
"OpenReportKey": "LeftControl + P"
```

On Mac keyboards, `Alt` means `Option`.

Health insurance values are currently handled in code and may be expanded later.

## Current design boundaries

Reality Check 1.0 focuses on taxes, health insurance, debt handling, and financial reporting.

The following are not included in 1.0:

- Banking system
- Loans
- Interest
- Investments
- Stock or futures mechanics
- Dynamic market price volatility
- Historical replay of old custom tax notices
- Custom insurance claim UI

## Roadmap

Planned direction for future versions:

### Reality Check 2.0

Planned systems may include:

- Banking system
- Loans and interest
- Savings or investment mechanics
- Market price volatility
- Long-term pressure against one endlessly optimal production strategy

These systems are not promised in exact form yet. They are the intended direction for future development.

## Known limitations

- Historical vanilla mail replay does not reopen the custom tax notice UI.
- Tax notices are intended to be read when delivered.
- Historical tax information is available through Tax Report and financial reports.
- Harvey medical insurance claims currently use vanilla mail, not a custom UI.
- The mod is currently designed around English UI text.

## Permissions

Please do not reupload this mod or modified versions without permission.

You may inspect the source code for learning purposes. Translation patches, compatibility patches, or modified releases should request permission first.

## Credits

Created by Stan.
