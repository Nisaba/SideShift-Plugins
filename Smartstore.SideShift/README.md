# SmartStore SideShift Payment Plugin

A **SmartStore payment integration** for [SideShift.ai](https://sideshift.ai) – a non-custodial crypto swap service.  
This plugin enables SmartStore merchants to accept payments in a wide range of cryptocurrencies through the SideShift API.

---

## Project Vision

This plugin is part of a broader initiative to provide **SideShift payment plugins for the most widely used e-commerce platforms**.  
SmartStore is the first step: a powerful, ASP.NET-based e-commerce solution widely adopted by merchants who require performance, flexibility, and extensibility.

By integrating SideShift, SmartStore merchants can **accept dozens of cryptocurrencies with no manual setup**.

---

## Features

- **Seamless SideShift Integration**  
  Accept cryptocurrency payments via SideShift directly in your SmartStore checkout.

- **Automatic Webhook Setup**  
  When plugin settings are saved:
  - If no webhook exists for this store, the plugin creates it automatically.  
  - Ensures crypto/network validity by checking against SideShift’s supported pairs.  
  - Retrieves the number of required decimals for accurate amount calculation.  
    - If SideShift does not return decimal precision (e.g., `BTC/bitcoin`), the plugin defaults to **8 decimals**.

- **Fiat → Crypto Conversion**  
  SideShift does **not support fiat conversion directly**.  
  To solve this, the plugin uses [CryptoCompare](https://www.cryptocompare.com) to calculate prices in the merchant’s chosen crypto from SmartStore’s fiat prices.

- **Order Notes with Swap Tracking**  
  The SideShift swap ID is recorded in SmartStore order notes, ensuring full traceability.

- **Automated Payment Confirmation**  
  Once the customer completes the payment, the webhook triggers order updates inside SmartStore.  
  _(Note: SideShift currently does not secure webhook requests, a limitation outside the plugin’s scope.)_

---

## Installation & Setup (for developpers)

1. Clone or download the repository:
   ```bash
   git clone https://github.com/Nisaba/SideShift-Plugins.git

2. Copy the plugin folder into your SmartStore solution under:
 ```bash
/Plugins/Payments/Smartstore.SideShift
```

3. Rebuild the SmartStore project.

4. Enable the plugin from SmartStore Admin → Configuration → Plugins.

## Installation & Setup (for merchants)

1. In your admin panel of your SmartStore instance, go to Plugins / Manage Plugins
2. Upload the pluin zip file
3. Click on Edit / Reload list of plugin
4. The plugin is now displayed on the plugin list. Click on install. It can be configured now.

Note: Once it is stabilized, this plugin can be published in the official SmartStore plugin store, for better visibility.

## Configure SideShift settings

1. Select your payout crypto and network.

2. Ensure that the crypto pair is supported by SideShift.

3. Save the settings – this will automatically create the webhook.


## Important Considerations

- **Same-Crypto Payments: **
SideShift currently does not allow swaps with the same crypto (e.g., paying BTC when the merchant also requests BTC).
This raises an open design question:
Should the plugin bypass SideShift in this case and treat it as a direct crypto payment?
Or should merchants restrict configurations to pairs that avoid this scenario?

- **Webhook Security: **
SideShift’s API does not provide webhook authentication at this time.
Merchants should be aware of this limitation when relying on automated callbacks.

## Roadmap

 - Extend plugin functionality for more complex fiat/crypto scenarios. Ex: refunds

 - Explore fallback handling for same-crypto payments.

 - Develop additional plugins for other major e-commerce platforms (Magento, WooCommerce, Prestashop, etc.) as part of this initiative.

## Contribution

Contributions, bug reports, and ideas are welcome!
This project aims to expand crypto adoption by making SideShift accessible to as many merchants as possible.

## License

MIT License. See LICENSE for details.
