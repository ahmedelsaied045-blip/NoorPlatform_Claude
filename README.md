# Noor Platform (نور)

An Arabic, right-to-left (RTL) eCommerce storefront for a lighting brand — chandeliers, lamps, and LED lighting — built on top of [nopCommerce 4.90.4](https://www.nopcommerce.com/) with a custom theme, **NoorTheme1**.

> أضئ مساحتك بلمسة من نور — *Light up your space with a touch of Noor.*

## Overview

| | |
|---|---|
| **Base platform** | nopCommerce 4.90.4 (ASP.NET Core) |
| **Runtime** | .NET 9 (SDK `9.0.100`+) |
| **Database** | Microsoft SQL Server 2012+ (also supports MySQL / PostgreSQL) |
| **Custom theme** | `NoorTheme1` — RTL, Arabic, golden accent |
| **Cache (optional)** | Redis |

## What's custom here

This repo is a standard nopCommerce source tree plus a bespoke theme. The Noor-specific work lives under:

```
src/Presentation/Nop.Web/Themes/NoorTheme1/
├── theme.json                          # Theme metadata (RTL enabled)
├── Content/css/noor-lighting.css       # Brand styling
└── Views/Shared/
    ├── _NoorHeroBanner.cshtml          # Homepage hero ("تسوق الآن" → search)
    └── _NoorLanding.cshtml             # Custom landing sections
```

The hero banner's primary call-to-action ("تسوق الآن" / *Shop now*) links to the catalog search page.

## Getting started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- SQL Server (Express is fine) running locally or reachable
- (Optional) Redis, if you enable distributed caching

### Run locally

```bash
cd src/Presentation/Nop.Web
dotnet run
```

Then open the HTTPS URL printed in the console (e.g. `https://localhost:5001`).

On first launch, nopCommerce shows an installation wizard where you provide your database connection and admin credentials. After installation, switch the active theme to **NoorTheme1** in:

**Admin → Configuration → Settings → General settings → Theme**

### Configuration & secrets

Local configuration (database connection string, Redis, etc.) lives in
`src/Presentation/Nop.Web/App_Data/appsettings.json`. **This file is gitignored and is never committed** — each environment supplies its own.

## Project structure

```
src/
├── Libraries/        # Nop.Core, Nop.Data, Nop.Services — domain & data layers
├── Presentation/
│   ├── Nop.Web/              # Public storefront (MVC) + Admin area
│   └── Nop.Web.Framework/    # Shared web infrastructure
├── Plugins/          # Payment, shipping, widget, and other plugins
└── Tests/            # Unit tests
```

## Credits

Built on [nopCommerce](https://www.nopcommerce.com/) — a free, open-source ASP.NET Core eCommerce platform © Nop Solutions, Ltd. See `LICENSE.md` for the nopCommerce Public License.

The **Noor Platform** theme and customizations are maintained in this repository.
