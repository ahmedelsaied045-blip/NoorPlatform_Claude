using Nop.Plugin.Misc.NoorAiAssistant.Domain;

namespace Nop.Plugin.Misc.NoorAiAssistant.Data;

/// <summary>
/// The knowledge base the assistant ships with.
/// </summary>
/// <remarks>
/// These are starting points, not final copy — every one of them is editable in the admin, and the store
/// owner is expected to correct the specifics (delivery windows, branch addresses, phone numbers) to match
/// reality. They exist so that a freshly installed assistant answers the nine questions every lighting
/// store gets asked, instead of shrugging until someone finds time to write a FAQ.
///
/// The Keywords column is what actually routes a question here, so each entry carries the words a shopper
/// would really use, in both languages, including the colloquial ones.
/// </remarks>
public static class NoorAiSeedData
{
    /// <summary>
    /// Gets the starter FAQ entries.
    /// </summary>
    /// <param name="englishLanguageId">The store's English language, or 0 when it has none</param>
    /// <param name="arabicLanguageId">The store's Arabic language, or 0 when it has none</param>
    public static IEnumerable<ChatFaq> Faqs(int englishLanguageId, int arabicLanguageId)
    {
        var entries = new List<ChatFaq>();
        var order = 0;

        void Add(FaqTopic topic, string keywords, string questionEn, string answerEn, string questionAr, string answerAr)
        {
            order += 10;

            entries.Add(new ChatFaq
            {
                Topic = topic,
                Keywords = keywords,
                Question = questionEn,
                Answer = answerEn,
                LanguageId = englishLanguageId,
                DisplayOrder = order,
                Published = true
            });

            //a store with no Arabic installed gets the English entries only
            if (arabicLanguageId > 0)
            {
                entries.Add(new ChatFaq
                {
                    Topic = topic,
                    Keywords = keywords,
                    Question = questionAr,
                    Answer = answerAr,
                    LanguageId = arabicLanguageId,
                    DisplayOrder = order,
                    Published = true
                });
            }
        }

        Add(FaqTopic.Shipping,
            "shipping,ship,delivery charge,delivery cost,shipping cost,courier,شحن,توصيل,رسوم الشحن,تكلفة الشحن",
            "How much does shipping cost?",
            "**Shipping is free on orders over 500 SAR.** Below that, a flat rate of 25 SAR applies anywhere in the Kingdom.\n\nLarge fixtures such as chandeliers are shipped with protective packaging at no extra charge.",
            "كم تكلفة الشحن؟",
            "**الشحن مجاني للطلبات فوق 500 ريال.** وما دون ذلك، الرسوم ثابتة 25 ريال لجميع مناطق المملكة.\n\nالقطع الكبيرة مثل الثريات تُشحن بتغليف واقٍ دون رسوم إضافية.");

        Add(FaqTopic.DeliveryTime,
            "delivery time,how long,when will,arrive,shipping time,eta,مدة التوصيل,متى يصل,كم يستغرق,وقت التوصيل",
            "How long does delivery take?",
            "- **Riyadh, Jeddah, Dammam:** 1–3 business days\n- **Other cities:** 3–5 business days\n- **Made-to-order chandeliers:** 2–3 weeks\n\nYou'll get a tracking link by SMS as soon as your order ships.",
            "كم تستغرق مدة التوصيل؟",
            "- **الرياض وجدة والدمام:** من 1 إلى 3 أيام عمل\n- **باقي المدن:** من 3 إلى 5 أيام عمل\n- **الثريات المصنوعة حسب الطلب:** من أسبوعين إلى ثلاثة\n\nسيصلك رابط التتبع عبر رسالة نصية فور شحن طلبك.");

        Add(FaqTopic.Returns,
            "return,returns,refund,exchange,money back,cancel order,إرجاع,ارجاع,استرجاع,استبدال,إلغاء الطلب,مرتجع",
            "What is your return policy?",
            "You can return any item **within 14 days** of delivery, provided it is unused and in its original packaging.\n\nRefunds are issued to the original payment method within 5–7 business days of us receiving the item. Installed or custom-made items cannot be returned.",
            "ما هي سياسة الإرجاع؟",
            "يمكنك إرجاع أي منتج **خلال 14 يومًا** من الاستلام، بشرط أن يكون غير مستخدم وفي عبوته الأصلية.\n\nيتم رد المبلغ إلى وسيلة الدفع الأصلية خلال 5 إلى 7 أيام عمل من استلامنا للمنتج. لا يمكن إرجاع المنتجات المركّبة أو المصنوعة حسب الطلب.");

        Add(FaqTopic.Warranty,
            "warranty,guarantee,defect,broken,faulty,ضمان,كفالة,عطل,معطل,تالف",
            "What warranty do you offer?",
            "- **LED fixtures and drivers:** 2 years\n- **Chandeliers and decorative lighting:** 1 year\n- **Switches, sockets and accessories:** 1 year\n- **CCTV cameras:** 2 years\n\nThe warranty covers manufacturing defects. Keep your invoice — it's all you need to make a claim.",
            "ما هو الضمان الذي تقدمونه؟",
            "- **وحدات الليد والمحولات:** سنتان\n- **الثريات والإنارة الديكورية:** سنة واحدة\n- **المفاتيح والأفياش والملحقات:** سنة واحدة\n- **كاميرات المراقبة:** سنتان\n\nيغطي الضمان عيوب التصنيع. احتفظ بالفاتورة — فهي كل ما تحتاجه لتقديم المطالبة.");

        Add(FaqTopic.PaymentMethods,
            "payment,pay,mada,visa,mastercard,apple pay,tamara,tabby,cash on delivery,cod,installment,دفع,الدفع,فيزا,مدى,تقسيط,الدفع عند الاستلام",
            "What payment methods do you accept?",
            "We accept **mada, Visa, Mastercard, Apple Pay** and **bank transfer**.\n\n**Cash on delivery** is available for orders under 3,000 SAR. Instalment plans through **Tamara** and **Tabby** are available at checkout.",
            "ما هي طرق الدفع المتاحة؟",
            "نقبل **مدى، فيزا، ماستركارد، Apple Pay** و**التحويل البنكي**.\n\n**الدفع عند الاستلام** متاح للطلبات أقل من 3,000 ريال. كما تتوفر خطط التقسيط عبر **تمارا** و**تابي** عند إتمام الطلب.");

        Add(FaqTopic.Branches,
            "branch,branches,showroom,store location,address,where are you,visit,فرع,فروع,معرض,المعرض,العنوان,الموقع,وين مكانكم",
            "Where are your branches?",
            "- **Riyadh:** Exit 9, Eastern Ring Road\n- **Jeddah:** Prince Sultan Street, Al Zahra\n- **Dammam:** King Fahd Road, Al Faisaliyah\n\nAll three showrooms display our full chandelier and outdoor lighting range.",
            "أين تقع فروعكم؟",
            "- **الرياض:** مخرج 9، الدائري الشرقي\n- **جدة:** شارع الأمير سلطان، حي الزهراء\n- **الدمام:** طريق الملك فهد، حي الفيصلية\n\nجميع المعارض الثلاثة تعرض تشكيلتنا الكاملة من الثريات والإنارة الخارجية.");

        Add(FaqTopic.WorkingHours,
            "working hours,opening hours,open,close,timing,when are you open,ساعات العمل,مواعيد العمل,متى تفتحون,الدوام",
            "What are your working hours?",
            "- **Saturday to Thursday:** 9:00 AM – 11:00 PM\n- **Friday:** 4:00 PM – 11:00 PM\n\nOur online store never closes, and I'm here around the clock.",
            "ما هي ساعات العمل؟",
            "- **السبت إلى الخميس:** من 9 صباحًا حتى 11 مساءً\n- **الجمعة:** من 4 عصرًا حتى 11 مساءً\n\nمتجرنا الإلكتروني لا يغلق أبدًا، وأنا هنا على مدار الساعة.");

        Add(FaqTopic.ContactInformation,
            "contact,phone,call,whatsapp,email,support,help desk,اتصال,تواصل,رقم,جوال,واتساب,ايميل,البريد,الدعم",
            "How can I contact you?",
            "- **Phone / WhatsApp:** 920 000 000\n- **Email:** support@noor.example\n\nOur team answers within a few hours during working hours. For a technical lighting question, tell me here — I can usually answer it right away.",
            "كيف يمكنني التواصل معكم؟",
            "- **الهاتف / واتساب:** 920 000 000\n- **البريد الإلكتروني:** support@noor.example\n\nيرد فريقنا خلال ساعات قليلة أثناء ساعات العمل. أما الأسئلة الفنية عن الإنارة، فاسألني هنا — غالبًا سأجيبك فورًا.");

        Add(FaqTopic.CompanyInformation,
            "about,who are you,company,noor,about us,من انتم,عن الشركة,نور,من نحن",
            "Who is Noor?",
            "Noor is a Saudi lighting specialist. We supply **chandeliers, indoor and outdoor lighting, LED strips, spotlights, switches, sockets and CCTV** to homes, villas and projects across the Kingdom.\n\nWe don't just sell fixtures — tell me your room size and I'll plan the lighting for you.",
            "من هي نور؟",
            "نور متخصصة سعودية في الإنارة. نوفّر **الثريات والإنارة الداخلية والخارجية وشرائط الليد والسبوت لايت والمفاتيح والأفياش وكاميرات المراقبة** للمنازل والفلل والمشاريع في جميع أنحاء المملكة.\n\nنحن لا نبيع الإنارة فحسب — أخبرني بمقاس غرفتك وسأخطط لك الإنارة بالكامل.");

        return entries;
    }

    /// <summary>
    /// Gets the starter chips shown in an empty conversation. They double as documentation: each one
    /// demonstrates a different thing the assistant can do, so a shopper discovers the room planner and
    /// the comparison without being told they exist.
    /// </summary>
    public static IEnumerable<SuggestedQuestion> SuggestedQuestions(int englishLanguageId, int arabicLanguageId)
    {
        var questions = new List<SuggestedQuestion>();
        var order = 0;

        void Add(string english, string arabic)
        {
            order += 10;

            questions.Add(new SuggestedQuestion
            {
                Text = english,
                LanguageId = englishLanguageId,
                DisplayOrder = order,
                Published = true
            });

            if (arabicLanguageId > 0)
            {
                questions.Add(new SuggestedQuestion
                {
                    Text = arabic,
                    LanguageId = arabicLanguageId,
                    DisplayOrder = order,
                    Published = true
                });
            }
        }

        Add("I need a chandelier", "أحتاج ثريا");
        Add("My room is 5 x 4 — what lighting do I need?", "غرفتي 5 × 4 — ما الإنارة التي أحتاجها؟");
        Add("I need outdoor lighting", "أحتاج إنارة خارجية");
        Add("How many spotlights do I need for a bedroom?", "كم سبوت لايت أحتاج لغرفة النوم؟");
        Add("I need CCTV", "أحتاج كاميرات مراقبة");
        Add("What is your delivery time?", "كم مدة التوصيل؟");

        return questions;
    }
}
