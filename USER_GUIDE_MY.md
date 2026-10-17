# Restaurant POS အသုံးပြုသူလမ်းညွှန် (မြန်မာဘာသာ)

ဤလမ်းညွှန်သည် လက်ရှိအသုံးပြုနိုင်သော **Windows laptop က host လုပ်ပြီး Android ဖုန်း၊ iPhone၊ tablet သို့မဟုတ် အခြား PC များက browser ဖြင့် ချိတ်ဆက်အသုံးပြုသည့်ဗားရှင်း** အတွက် ဖြစ်သည်။ Android ဖုန်းကိုယ်တိုင် host လုပ်မည့် APK ကို မထုတ်ပေးရသေးပါ။ လက်ရှိဗားရှင်းတွင် ဖုန်းက client အဖြစ်သာ သုံးနိုင်ပြီး host laptop ဖွင့်ထားရမည်။

စနစ်အတွင်း ဈေးနှုန်းနှင့် အစီရင်ခံစာငွေပမာဏကို **MMK (မြန်မာကျပ်)** ဖြင့် ဖော်ပြသည်။ အောက်တွင်ရေးထားသော `Overview`, `Orders` ကဲ့သို့သော အင်္ဂလိပ်စာများသည် ဝဘ်ဆိုက်ပေါ်တွင် မြင်ရမည့် ခလုတ်/စာမျက်နှာအမည်များ ဖြစ်သည်။

## ၁။ လိုအပ်သောပစ္စည်းများ

- Host အတွက် Windows 10/11 **64-bit laptop/PC** တစ်လုံး။ ဆိုင်ဖွင့်ချိန်တစ်လျှောက် ဖွင့်ထားပြီး sleep မဝင်အောင် ပြင်ထားပါ။
- Password ထည့်ထားသော ဆိုင်သုံး Wi-Fi တစ်ခု။ စက်အားလုံး တူညီသော Wi-Fi/network ပေါ်တွင် ရှိရမည်။ Internet ချိတ်ထားရန် **မလိုပါ**။
- Staff များအတွက် Android/iPhone/tablet/PC နှင့် browser။ Staff ဖုန်းများတွင် APK တပ်ဆင်ရန် မလိုပါ။
- ပုံမှန် backup ကို အခြားနေရာသို့ ကူးထားရန် USB drive သို့မဟုတ် အခြား storage။

**လုံခြုံရေး:** ဆိုင်ပိုင် Wi-Fi ကိုသာ `Private` network အဖြစ် အတည်ပြုပါ။ အများသုံး/Guest Wi-Fi တွင် POS ကို မဖွင့်ပါနှင့်။ Router တွင် port `5268` ကို Internet သို့ port-forward မလုပ်ပါနှင့်။ လက်ရှိ local ချိတ်ဆက်မှုသည် HTTP ဖြစ်သောကြောင့် ယုံကြည်ရသော၊ password ပါသော network တွင်သာ အသုံးပြုပါ။

## ၂။ Windows laptop တွင် ပထမဆုံး host လုပ်နည်း

1. [နောက်ဆုံးထုတ် Windows ZIP](https://github.com/Eulean/POSFORRESTURANT/releases/latest) ကို download လုပ်ပါ။ ZIP download လုပ်ချိန်တွင် Internet လိုနိုင်သော်လည်း တပ်ဆင်ပြီး ဆိုင်အတွင်းအသုံးပြုချိန်တွင် Internet မလိုပါ။
2. ZIP ကို laptop ၏ တည်ငြိမ်သော folder အသစ်ထဲသို့ **Extract All** လုပ်ပါ။ ZIP အတွင်းကနေ တိုက်ရိုက်ဖွင့်မသုံးပါနှင့်။
3. Laptop ကို ဆိုင်သုံး Wi-Fi နှင့် ချိတ်ပါ။ Extract လုပ်ထားသော folder ထဲရှိ `Start Restaurant POS.cmd` ကို နှစ်ချက်နှိပ်ပါ။ Command window ကို ဆိုင်ဖွင့်ချိန်တစ်လျှောက် မပိတ်ပါနှင့်။
4. ပထမဆုံးဖွင့်ချိန်တွင် Admin password အသစ်တစ်ခု တောင်းမည်။ **အနည်းဆုံး စာလုံး ၁၀ လုံး** ထည့်ပြီး မှတ်ထားပါ။ ပထမ Admin username သည် `admin` ဖြစ်သည်။
5. Windows က Wi-Fi ကို `Public` ဟု သတ်မှတ်ထားပါက “သင့်ဆိုင်၏ ယုံကြည်ရသော network လား” ဟု မေးမည်။ ကိုယ့်ဆိုင် Wi-Fi အမှန်ဖြစ်မှ `YES` ရိုက်ပါ။ ထို့နောက် Windows administrator/UAC အတည်ပြုချက်ကို ခွင့်ပြုပါ။ ဤအဆင့်မပြီးလျှင် laptop ပေါ်တွင်သာ သုံးနိုင်ပြီး အခြားဖုန်းများ မချိတ်နိုင်ပါ။
6. Browser တွင် `http://localhost:5268/host` ဖွင့်လာမည်။ Admin account ဖြင့် login ဝင်ပါ။ `Connect devices` စာမျက်နှာတွင် “ready” အစိမ်းရောင်အခြေအနေ၊ QR code နှင့် `http://...:5268/` လိပ်စာ မြင်ရပါက laptop က host လုပ်နေပြီဖြစ်သည်။

**နေ့စဉ်ဖွင့်ချိန်:** Laptop ကို ဆိုင် Wi-Fi နှင့်ချိတ်၊ `Start Restaurant POS.cmd` ကိုဖွင့်၊ browser မှ login ဝင်ပြီး `Connect devices` အခြေအနေကို စစ်ပါ။ Laptop ပိတ်သွားခြင်း၊ sleep ဝင်ခြင်း၊ Wi-Fi ပြောင်းခြင်းတို့ဖြစ်ပါက staff ဖုန်းများ ချိတ်ဆက်မှုပြတ်နိုင်သည်။

## ၃။ ဖုန်း၊ tablet နှင့် အခြား PC များ ချိတ်ဆက်နည်း

1. Host laptop နှင့် staff စက်ကို **Wi-Fi တစ်ခုတည်း** သို့ ချိတ်ပါ။ Guest Wi-Fi သီးသန့် network သို့မဟုတ် “client isolation” ဖွင့်ထားသော Wi-Fi မသုံးပါနှင့်။
2. Host laptop ပေါ်တွင် Admin က `Connect devices` ကိုဖွင့်ပါ။ QR code မပေါ်မီ အစိမ်းရောင် ready အခြေအနေ ရှိရမည်။
3. Staff ဖုန်း၏ Camera ဖြင့် QR code ကို scan လုပ်ပြီး ပေါ်လာသော link ကို browser တွင်ဖွင့်ပါ။ QR မဖတ်နိုင်ပါက စာမျက်နှာတွင် ပြထားသော `http://192.168.x.x:5268/` ပုံစံ လိပ်စာကို browser address bar ထဲ ရိုက်ထည့်ပါ။ **`localhost` ကို ဖုန်းပေါ်တွင် မရိုက်ပါနှင့်**—၎င်းသည် laptop ပေါ်တွင်သာ အလုပ်လုပ်သည်။
4. Admin ဖန်တီးပေးထားသော staff username/password ဖြင့် login ဝင်ပါ။ User တစ်ဦးစီကို ကိုယ်ပိုင် account ပေးသုံးပါ။
5. ဖုန်းနှင့် laptop နှစ်ခုလုံးတွင် လိပ်စာ တစ်ခုတည်း၏ POS ကို သုံးနေပါက အချက်အလက်များကို laptop ပေါ်ရှိ တစ်ခုတည်းသော database တွင် သိမ်းမည်။

Wi-Fi သို့မဟုတ် laptop IP ပြောင်းသွားပါက POS ကို ပိတ်ပြီး ပြန်ဖွင့်ပါ။ Admin က `Connect devices` တွင် `Refresh status` နှိပ်၍ QR code အသစ်ကို စစ်ပါ။

## ၄။ Account အမျိုးအစားနှင့် မြင်နိုင်သောစာမျက်နှာများ

| Role | အဓိကလုပ်နိုင်သောအလုပ် |
| --- | --- |
| `Admin` | စနစ်ပြင်ဆင်ခြင်း၊ menu/stock/report/user/backup စီမံခြင်း၊ order နှင့် payment လုပ်ခြင်း |
| `Waiter` | `Overview`, `Orders`, `Kitchen`, `Tables` ကိုသုံးပြီး စားပွဲရွေး order ဖန်တီးခြင်း |
| `Cashier` | `Overview`, `Orders`, `Kitchen` ကိုသုံးပြီး ငွေပေးချေမှု၊ receipt လုပ်ခြင်း။ Order အသစ်ဖန်တီးခွင့်မှာ Waiter သို့မဟုတ် Admin အတွက်သာ ဖြစ်သည် |

`Kitchen` အတွက် သီးခြား role မရှိသေးပါ။ အထက်ပါ account များ login ဝင်ပြီး Kitchen စာမျက်နှာကို ကြည့်နိုင်သည်။ Admin သည် `Admin` စာမျက်နှာတွင် staff account များဖန်တီးကာ role သတ်မှတ်ရမည်။ Staff password အသစ်သည် အနည်းဆုံး စာလုံး ၆ လုံး ဖြစ်ရမည်။

## ၅။ စတင်ပြင်ဆင်ရန် အကြံပြုအစီအစဉ်

1. `Admin` → `Shop Location` တွင် ဆိုင်အမည်၊ ဖုန်း၊ လိပ်စာ၊ logo ထည့်ပါ။ Receipt/printout တွင် အသုံးပြုမည်။ `Save Location` နှိပ်ပါ။ Logo အတွက် `Upload Logo` ကို သီးသန့်နှိပ်ပါ။
2. `Admin` → `Create User` တွင် Waiter/Cashier account များ ဖန်တီးပါ။
3. `Tables` → စားပွဲအမည်နှင့် `Seats` (ထိုင်ခုံအရေအတွက်) ထည့်ပြီး `Add Table` နှိပ်ပါ။
4. `Menu` → `Categories` တွင် အစားအသောက်အုပ်စုများ ထည့်ပါ။ ထို့နောက် `New Menu Item` တွင် ဟင်းအမည်၊ ဖော်ပြချက်၊ MMK ဈေးနှုန်း၊ category နှင့် `Active` ကို သတ်မှတ်ပြီး `Save Menu Item` နှိပ်ပါ။ ဥပမာ ၅,၀၀၀ ကျပ်ဆို `5000` ထည့်ပါ။
5. Menu item ကို သိမ်းပြီးမှ item card အောက်ရှိ `Upload Image` ဖြင့် ပုံတင်ပါ။
6. Stock tracking သုံးလိုပါက `Inventory` တွင် ပါဝင်ပစ္စည်း၊ unit၊ လက်ကျန်၊ ပြန်မှာရမည့်အဆင့်နှင့် unit cost များ ထည့်ပါ။
7. စမ်းသပ် order တစ်ခုဖန်တီးပြီး staff ဖုန်းမှ ဝင်နိုင်ခြင်း၊ status ပြောင်းခြင်း၊ payment/receipt ထုတ်ခြင်းကို စစ်ပါ။ စမ်းသပ်ထားသော အရောင်းဒေတာသည် report ထဲပါနိုင်သဖြင့် လက်တွေ့အရောင်းမစမီ သီးသန့် စမ်းသပ်ပါ။

## ၆။ စာမျက်နှာနှင့် feature တစ်ခုချင်း အသုံးပြုနည်း

### `Overview` — လက်ရှိလုပ်ငန်းအခြေအနေ

`Open Orders` သည် မပြီးသေးသော order အရေအတွက်၊ `Tables Seated` သည် အသုံးပြုနေသောစားပွဲ/စားပွဲအားလုံး၊ `Kitchen Queue` သည် မီးဖိုဆောင်တွင်လုပ်နေသည့်နှင့် အဆင်သင့် order များ၊ `Inventory Alerts` သည် လက်ကျန်နည်းသောပစ္စည်းအရေအတွက်ကို ဖော်ပြသည်။ `Live Orders` တွင် order status စစ်ရန် filter ရွေးနိုင်ပြီး `Refresh` နှိပ်နိုင်သည်။ `Table Pulse` တွင် စားပွဲလွတ်/မလွတ် ကြည့်နိုင်ကာ `Manage Tables` ဖြင့် `Tables` သို့ သွားနိုင်သည်။

ဘယ်ဘက် menu အောက်ရှိ `Morning Flow`, `Open tables: 8`, `Orders: 14` သည် **လက်ရှိ live စာရင်းမဟုတ်သော အလှဆင်စာသား** ဖြစ်သည်။ အမှန်တကယ်အရေအတွက်အတွက် `Overview` ၏ အပေါ်ပိုင်းစာရင်းများကိုသာ ကြည့်ပါ။

### `Tables` — စားပွဲစီမံခြင်း

`New Table` တွင် `Name`, `Seats`, `Available` ဖြည့်ပြီး `Add Table` နှိပ်ပါ။ `Current Tables` တွင် `Available`/`Busy` ကို ကြည့်ပါ။ `Toggle` သည် လွတ်/မလွတ် အခြေအနေကို လက်ဖြင့် ပြောင်းသည်။ Order အသစ်တင်သောအခါ စားပွဲသည် Busy ဖြစ်ပြီး order ကို `Closed` သို့မဟုတ် `Cancelled` ပြောင်းသောအခါ Available ပြန်ဖြစ်သည်။ Busy စားပွဲဖြင့် order အသစ်ဖွင့်၍ မရနိုင်ပါ။

### `Menu` — ဟင်းလျာနှင့် ဈေးနှုန်း

Admin သာ ဝင်နိုင်သည်။ `Categories` တွင် အုပ်စုထည့်/ဖျက်နိုင်သည်။ `New Menu Item` တွင် `Name` မဖြစ်မနေ၊ `Description` ရွေးချယ်နိုင်၊ `Price` ကို MMK ဖြင့်၊ `Category` ကို ရွေးပြီး `Active` ဖွင့်/ပိတ်ကာ `Save Menu Item` နှိပ်ပါ။ Item card တွင် `Edit` ဖြင့် အမည်/ဖော်ပြချက်/ဈေး/Active ပြင်ပြီး `Save` နှိပ်နိုင်သည်။ `Delete` ဖျက်ပြီးချက်ချင်း `Undo` ပေါ်လာလျှင် ပြန်ယူနိုင်သည်။

`Upload Image` သည် JPEG/PNG/WebP ပုံကို လက်ခံပြီး server က WebP အဖြစ် ပြောင်းသိမ်းသည်။ တင်မည့်ပုံ 10 MB ထက် မကြီးရ။ Menu ပုံ၏ အရှည်ဆုံးဘက်ကို 1600 px အထိ ချုံ့သိမ်းသည်။ Logo ကိုလည်း WebP ပြောင်းပြီး 512 px အထိ ချုံ့သည်။ ပုံအဟောင်းများကို အလိုအလျောက် ပြန်မပြောင်းပါ။

**သတိ:** `Active` ပိတ်ထားသော item ကို လက်ရှိ `Orders` ရွေးချယ်စာရင်းက စစ်ထုတ်မထားသေးနိုင်ပါ။ ရောင်းမည့်ဟင်းလျာစာရင်းမှ အမှန်တကယ်ဖယ်လိုပါက staff ကို အသိပေးပြီး order screen ကို စစ်ပါ။

### `Orders` — Order ဖန်တီးခြင်းမှ ငွေရှင်းခြင်းအထိ

1. Waiter သို့မဟုတ် Admin က `Create Order` အပိုင်းတွင် `Table` ရွေးပါ။ `Add Items` တွင် ဟင်းလျာများနှိပ်ပါ။ `Selected` အောက်ရှိ `+`/`-` ဖြင့် အရေအတွက် ပြင်ပါ။ `Estimated total` ကို စစ်ပြီး `Create Order` နှိပ်ပါ။
2. Order status အစဉ်သည် `Open` → `InProgress` → `Ready` → `Served` → `Paid` → `Closed` ဖြစ်သည်။ `Advance` သည် နောက်အဆင့်သို့ ရွှေ့သည်။ `Cancel` သည် order ကို ပယ်ဖျက်သည်။ Status မမှားစေရန် တာဝန်ရှိသူကသာ ပြောင်းပါ။
3. Cashier/Admin က `Served` order တွင် `Pay` နှိပ်ပါ။ `Amount` ကို order စုစုပေါင်းနှင့် **တိတိကျကျတိုက်စစ်**၊ `Method` (`Cash`, `Card`, `Mobile`) ရွေး၊ လိုပါက `Reference` (transaction ID စသည်) ထည့်ပြီး `Confirm Payment` နှိပ်ပါ။ လက်ရှိ UI သည် ငွေပမာဏ တစ်စိတ်တစ်ပိုင်းသာ ထည့်ထားလျှင်လည်း order ကို ပိတ်နိုင်သောကြောင့် ငွေတကယ်ရပြီး ပမာဏပြည့်မပြည့်ကို အတည်ပြုမှ ခလုတ်နှိပ်ပါ။ အောင်မြင်လျှင် order ပိတ်ပြီး စားပွဲ Available ပြန်ဖြစ်သည်။
4. `Paid`/`Closed` order တွင် `Receipt` နှိပ်၍ PDF download လုပ်နိုင်ပြီး `Print` နှိပ်၍ browser print preview ဖွင့်နိုင်သည်။ ဖုန်းပေါ် print လုပ်ခြင်းသည် browser/ပရင်တာစီစဉ်ထားမှုအပေါ် မူတည်သည်။

`Current Orders` အကွက်တွင် နောက်ဆုံး order အချို့နှင့် လုပ်ဆောင်ရန်ခလုတ်များကို မြင်ရသည်။ အောက်ပိုင်း status အုပ်စုများတွင် order စုစုပေါင်းကို ကြည့်ပါ။ Cashier account ဖြင့် `Create Order` ခလုတ်ကို မြင်ရနိုင်သော်လည်း server က ခွင့်မပြုသောကြောင့် Waiter/Admin account ကို သုံးပါ။

**ငွေမရသေးဘဲ `Advance` ဖြင့် `Paid`/`Closed` သို့ မရွှေ့ပါနှင့်။** Status ပြောင်းခြင်းသည် ငွေပေးချေမှု record ဖန်တီးခြင်းမဟုတ်ပါ။ `Served` မှ `Pay` လမ်းကြောင်းကိုသာ သုံးပါ။

### `Kitchen` — မီးဖိုဆောင်အလုပ်စာရင်း

`InProgress` နှင့် `Ready` အုပ်စုနှစ်ခုကို ဖော်ပြသည်။ Order စတင်ပြင်ဆင်သည့်အခါ Orders စာမျက်နှာမှ `Open` ကို `InProgress` သို့ ရွှေ့ပါ။ ပြီးလျှင် Kitchen တွင် `Mark Ready` နှိပ်ပါ။ Customer ထံ ပို့ပြီးလျှင် `Mark Served` နှိပ်ပါ။ `Served` သို့ ရွှေ့ရာတွင် ဟင်းလျာနှင့် ingredient recipe ချိတ်ထားမှသာ stock အလိုအလျောက် လျော့မည်။ လက်ရှိ web UI တွင် recipe ချိတ်ရန် သီးခြား form မရှိသေးသဖြင့် stock ကို အလိုအလျောက်လျော့မည်ဟု မယူဆပါနှင့်။ Stock ကို `Inventory` တွင် စစ်/ပြင်ပါ။

### `Inventory` — ကုန်ကြမ်းလက်ကျန်

`Add Ingredient` တွင် အမည်၊ `Unit` (ဥပမာ `kg`, `L`, `each`), လက်ရှိ `Stock`, `Reorder Level`, `Cost Per Unit` (MMK) နှင့် `Active` ဖြည့်ပါ။ `All Ingredients` တွင် လက်ကျန်၊ unit cost၊ `Good`/`Low` ကို ကြည့်ပြီး `Edit`/`Delete` လုပ်နိုင်သည်။ လက်ကျန်သည် reorder level နှင့် တူသို့မဟုတ် နိမ့်ပါက `Low Stock Alerts` တွင် ပေါ်မည်။

`Adjust Stock` တွင် ပစ္စည်းရွေး၊ `Quantity Change` တွင် ပစ္စည်းဝင်လာလျှင် အပေါင်းကိန်း (ဥပမာ `+5`), ပျက်စီး/သုံးစွဲသွားလျှင် အနှုတ်ကိန်း (ဥပမာ `-2`) ထည့်ပါ။ `Reason` ကို မဖြစ်မနေ ဖြည့်ပြီး `Apply Adjustment` နှိပ်ပါ။

**သတိ:** `Set Sample Costs` သည် unit cost မရှိသောပစ္စည်းများထဲသို့ စမ်းသပ်တန်ဖိုးများ ထည့်နိုင်သည်။ စီးပွားရေးလုပ်ငန်းအစစ်တွင် **မနှိပ်ပါနှင့်**။ Unit cost မမှန်ပါက အမြတ်အစီရင်ခံစာလည်း မမှန်နိုင်ပါ။

### `Reports` — အရောင်းနှင့် အမြတ်အစီရင်ခံစာ

Admin သာ ဝင်နိုင်သည်။ `Daily Summary` တွင် ရက်ရွေး၍ order အရေအတွက်၊ `Total Sales`, `Ingredient Cost`, `Profit`, ရောင်းအကောင်းဆုံးဟင်းလျာ၊ payment method အလိုက် ငွေဝင်မှုနှင့် အလုပ်အများဆုံးနာရီကို ကြည့်ပါ။ `Export CSV` သည် ရွေးထားသောနေ့၏ report ကို CSV ဖိုင်ထုတ်သည်။ `Close Day` သည် **ထိုနေ့ report snapshot ကို သိမ်းခြင်းသာ** ဖြစ်ပြီး POS server ကို မပိတ်သလို နောက်ထပ် order တင်ခြင်းကိုလည်း မတားပါ။

`Monthly Report` တွင် စတင်ရက်နှင့် ဆုံးရက် ရွေး၍ ကာလတစ်ခုလုံး၏ အရောင်း/ကုန်ကျ/အမြတ်၊ အရောင်းအကောင်းဆုံး item၊ payment method အလိုက်ဝင်ငွေနှင့် `Daily Breakdown` ကို ကြည့်ပါ။ `Export Excel` သည် `.xlsx` ဖိုင်ထုတ်သည်။ Report တွင် UTC ရက်စွဲအခြေခံထားသဖြင့် မြန်မာအချိန် ညသန်းခေါင်အနီး အရောင်းများ၏ရက်စွဲကို ဂရုစိုက်စစ်ဆေးပါ။ Ingredient cost/profit တိကျရန် စစ်မှန်သော cost နှင့် recipe data လိုအပ်သည်။

### `Admin` — ဝန်ထမ်းနှင့် ဆိုင်အချက်အလက်

`Create User` တွင် username, display name, password, role ရွေး၍ account အသစ်ဖန်တီးပါ။ `Reset Password` တွင် user ရွေးပြီး password အသစ်ပေးပါ။ `User Roles` တွင် role ပြောင်းနိုင်သည်။ `Shop Location` တွင် ဆိုင်အမည်/ဖုန်း/လိပ်စာ ဖြည့်ပြီး `Save Location` နှိပ်ပါ။ Logo ကို `Upload Logo` ဖြင့် သီးသန့်တင်ပါ။ Logo ထည့်ရန် UI label တွင် `PNG` ဟု ရေးထားနိုင်သော်လည်း JPEG, PNG, WebP ကို လက်ခံသည်။

### `Connect devices` — ဖုန်းချိတ်ဆက်ခြင်းနှင့် backup

Admin သာ ဝင်နိုင်သည်။ အစိမ်းရောင် ready ဖြစ်မှ QR code နှင့် staff URL ပြမည်။ QR code ကို ဖုန်း camera ဖြင့် scan လုပ်ပါ။ `Copy address` ဖြင့် link ကူးနိုင်ပြီး `Refresh status` ဖြင့် network အခြေအနေပြန်စစ်နိုင်သည်။ အနီရောင် “Other devices cannot connect yet” ပြပါက အောက်ပါ Troubleshooting ကို ကြည့်ပါ။ `Create backup now` နှိပ်လျှင် database နှင့်ပုံများပါသော ZIP backup ဖန်တီးပြီး laptop ပေါ်တွင် သိမ်းသည့် လိပ်စာကို ပြမည်။

### အပေါ်ဘားရှိ ခလုတ်များ

`Light`/`Dark` သည် အရောင် theme ပြောင်းသည်။ `Tablet On`/`Tablet Off` သည် tablet အတွက် UI mode ပြောင်းသည်။ `Log out` သည် လက်ရှိ account မှ ထွက်သည်။ Waiter/Admin အတွက် `New Order` shortcut ရှိသည်။

## ၇။ Backup၊ ပိတ်ခြင်းနှင့် version အသစ်တင်ခြင်း

- Database, menu ပုံများ၊ logo နှင့် backup များ၏ အခြေခံ folder သည် Windows ၏ `%LOCALAPPDATA%\RestaurantPos` ဖြစ်သည်။ File Explorer address bar ထဲ ဤလိပ်စာကို ရိုက်၍ ဖွင့်နိုင်သည်။
- Server စတင်ချိန်နှင့် ၂၄ နာရီခြားတစ်ကြိမ် backup ZIP အလိုအလျောက် ဖန်တီးရန် ကြိုးစားသည်။ အရေးကြီးသောအလုပ်ပြီးတိုင်း Admin က `Connect devices` → `Create backup now` နှိပ်နိုင်သည်။ Backup ZIP များကို `%LOCALAPPDATA%\RestaurantPos\Backups` တွင် ရှာနိုင်သည်။ USB/အခြားစက်သို့ ပုံမှန်ကူးထားပါ။ Laptop တစ်လုံးတည်းတွင်ထားသော backup သည် laptop ပျက်လျှင် မကာကွယ်နိုင်ပါ။
- POS ပိတ်ရန် app folder ထဲရှိ `Stop Restaurant POS.cmd` ကို နှစ်ချက်နှိပ်ပါ။ Host ပိတ်သွားလျှင် staff ဖုန်းများလည်း သုံးမရတော့ပါ။
- Version အသစ်တင်မီ backup ယူပါ။ အဟောင်း folder ထဲရှိ `Stop Restaurant POS.cmd` ဖြင့် server ပိတ်ပါ။ ZIP အသစ်ကို **folder အသစ်** ထဲ extract လုပ်ပြီး ထို folder မှ `Start Restaurant POS.cmd` ဖွင့်ပါ။ `%LOCALAPPDATA%\RestaurantPos` data folder ကို **မဖျက်ပါနှင့်**။ App update လုပ်ရာတွင် data folder ကို ဆက်သုံးသည်။
- Backup ZIP မှ ပြန်သွင်းရန် UI wizard မရှိသေးပါ။ ပြန်သွင်းရန်လိုလျှင် မူရင်း data folder နှင့် backup ZIP ကို မဖျက်ဘဲ နည်းပညာတာဝန်ခံနှင့် ဆက်သွယ်ပါ။

## ၈။ အဖြစ်များသောပြဿနာများ

**ဖုန်းက QR scan လုပ်သော်လည်း မဖွင့်ရ:** Host laptop နှင့် ဖုန်း Wi-Fi တူ/မတူ စစ်ပါ။ ဖုန်း၏ mobile data အစား Wi-Fi ကို သုံးကြည့်ပါ။ Guest Wi-Fi/client isolation မဟုတ်ကြောင်း စစ်ပါ။ Laptop ပေါ် `Connect devices` တွင် အစိမ်းရောင် ready ရှိရမည်။ `Public` Wi-Fi အတွက် launcher ၏ `YES` နှင့် Windows administrator prompt ကို အတည်ပြုပါ။ Windows Firewall rule မပြီးပါက POS ကို ပြန်ဖွင့်ပါ။ Router/Wi-Fi ပြောင်းထားလျှင် host ကို restart လုပ်၍ QR အသစ် scan လုပ်ပါ။

**Laptop ပေါ်မှာရပေမယ့် အခြားစက်မှာမရ:** ဖုန်း browser တွင် `localhost` မဟုတ်ဘဲ QR ပေါ်ရှိ laptop IP URL ကို သုံးပါ။ Host ၏ command window ပိတ်မထားရ။ စားသောက်ဆိုင် Wi-Fi သည် စက်အချင်းချင်းဆက်သွယ်ခွင့်ပေးရမည်။

**Login မဝင်နိုင်:** Username/password စစ်ပါ။ Admin password မေ့ပါက launcher က အဟောင်း password ကို မပြနိုင်ပါ။ Staff password ကို Admin က `Admin` → `Reset Password` တွင် ပြန်သတ်မှတ်နိုင်သည်။ မိမိ role မပါသောစာမျက်နှာကို မမြင်ရပါ။

**ပုံတင်ပြီး ပြန်မပေါ်:** နောက်ဆုံး ZIP ဖြင့် host လုပ်နေကြောင်း စစ်ပါ။ Browser ကို refresh လုပ်ပါ။ ပုံအမျိုးအစား JPEG/PNG/WebP ဖြစ်ပြီး 10 MB ထက် မကြီးရ။ Menu item ကို အရင်သိမ်းပြီးမှ ပုံတင်ပါ။ အဟောင်း version မှာတင်ထားသောပုံ ပျောက်နေပါက data folder မဖျက်ထားကြောင်း စစ်ပါ။

**Order အသစ်တင်မရ:** Waiter/Admin account သုံးပါ။ စားပွဲ `Available` ဖြစ်ရမည်၊ ဟင်းလျာ အနည်းဆုံးတစ်ခု ရွေးရမည်။ Cashier account ဖြင့် order အသစ် မဖန်တီးနိုင်ပါ။

**Backup မတွေ့:** `Connect devices` တွင် `Create backup now` နှိပ်ပြီး ပြထားသော path ကို ဖတ်ပါ။ `%LOCALAPPDATA%\RestaurantPos\Backups` ကို စစ်ပါ။ Backup မဖန်တီးနိုင်သည့် error ရှိပါက data folder/disk space ကို စစ်ပြီး နည်းပညာတာဝန်ခံထံ အကူအညီတောင်းပါ။

## ၉။ ဖုန်းကိုယ်တိုင် host လုပ်မည့် APK အကြောင်း

ယခုလမ်းညွှန်သည် **Windows host + ဖုန်း browser client** အတွက်သာ ဖြစ်သည်။ Android ဖုန်းတစ်လုံးက server/database ကို ကိုယ်တိုင်ထိန်းပြီး အခြားဖုန်း/PC များအား ဝန်ဆောင်မှုပေးမည့် APK ကို လက်ရှိ release တွင် **မပါသေးပါ**။ ထို့ကြောင့် laptop မပါဘဲ အသုံးပြုနိုင်သည်ဟု မယူဆပါနှင့်။ Android-host ဗားရှင်း အမှန်တကယ်ထုတ်ပြီး စမ်းသပ်ပြီးမှ ၎င်းအတွက် သီးသန့်လမ်းညွှန် ထပ်ရေးရမည်။
