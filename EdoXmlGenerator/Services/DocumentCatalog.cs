using EdoXmlGenerator.Models.Catalog;

namespace EdoXmlGenerator.Services;

/// <summary>
/// Каталог типов документов: категории и описания динамических форм.
/// Полностью отражает структуру типовых форм РФ (Продажи, Акты, Договоры, Касса,
/// Управление, Платежи, Регистрация и налоги, Почта).
/// </summary>
public static class DocumentCatalog
{
    public static IReadOnlyList<DocumentDefinition> GetDocuments() => _documents;

    public static IReadOnlyList<string> GetCategories() =>
        _documents.Select(d => d.Category).Distinct().ToList();

    public static DocumentDefinition? Find(string id) =>
        _documents.FirstOrDefault(d => d.Id == id);

    private static readonly List<DocumentDefinition> _documents = Build();

    private static DocumentDefinition Doc(string id, string title, string category, string desc, string root) =>
        new(id, title, category, desc) { RootElement = root };

    private static void Section(DocumentDefinition d, string title, params FieldDef[] fields)
    {
        var s = new DocSection(title);
        foreach (var f in fields) s.Fields.Add(f);
        d.Sections.Add(s);
    }

    private static FieldDef T(string key, string label, bool required = false, int maxLen = 0) =>
        new() { Key = key, Label = label, Kind = FieldKind.Text, Required = required, MaxLength = maxLen };

    private static FieldDef A(string key, string label, bool required = false, int rows = 3) =>
        new() { Key = key, Label = label, Kind = FieldKind.TextArea, Required = required, Rows = rows };

    private static FieldDef D(string key, string label, bool required = false) =>
        new() { Key = key, Label = label, Kind = FieldKind.Date, Required = required };

    private static FieldDef N(string key, string label, bool required = false) =>
        new() { Key = key, Label = label, Kind = FieldKind.Number, Required = required };

    private static FieldDef M(string key, string label, bool required = false) =>
        new() { Key = key, Label = label, Kind = FieldKind.Money, Required = required };

    private static FieldDef B(string key, string label) =>
        new() { Key = key, Label = label, Kind = FieldKind.Bool };

    // Общие блоки реквизитов, переиспользуемые в разных документах
    private static void AddDocumentBase(DocumentDefinition d)
    {
        Section(d, "Реквизиты документа",
            T("docNumber", "Номер документа", true),
            D("docDate", "Дата составления", true));
    }

    private static void AddParties(DocumentDefinition d, string seller = "Поставщик", string buyer = "Покупатель",
        bool withBank = false, bool withContract = false)
    {
        Section(d, seller,
            T("sellerName", $"Наименование ({seller})", true),
            T("sellerInn", "ИНН"),
            T("sellerKpp", "КПП"),
            T("sellerOkpo", "ОКПО"),
            T("sellerAddress", "Адрес", false, 0),
            T("sellerPhone", "Телефон"),
            T("sellerEmail", "Email"));

        Section(d, buyer,
            T("buyerName", $"Наименование ({buyer})", true),
            T("buyerInn", "ИНН"),
            T("buyerKpp", "КПП"),
            T("buyerOkpo", "ОКПО"),
            T("buyerAddress", "Адрес"),
            T("buyerPhone", "Телефон"),
            T("buyerEmail", "Email"));

        if (withContract)
        {
            Section(d, "Основание (договор/соглашение)",
                T("contractNo", "Номер договора"),
                D("contractDate", "Дата договора"),
                T("contractName", "Наименование договора"));
        }

        if (withBank)
        {
            Section(d, "Банковские реквизиты получателя",
                T("bankBik", "БИК банка"),
                T("bankName", "Название банка"),
                T("bankAccount", "Расчётный счёт"),
                T("bankCorr", "Корр. счёт"),
                T("bankLocation", "Местонахождение банка"));
        }
    }

    private static void AddItems(DocumentDefinition d, string title = "Товары (работы, услуги)")
    {
        Section(d, title,
            T("itemsTable", "Перечень позиций — см. таблицу внизу формы", false, 0));
    }

    private static void AddVatBlock(DocumentDefinition d)
    {
        Section(d, "НДС и итоговые суммы",
            B("vatEnabled", "Облагается НДС"),
            T("vatRate", "Ставка НДС (%)"),
            M("totalWithoutVat", "Сумма без НДС (руб.)"),
            M("totalVat", "Сумма НДС (руб.)"),
            M("total", "Итого с НДС (руб.)"));
    }

    private static void AddSignerBlock(DocumentDefinition d, string position = "Генеральный директор")
    {
        Section(d, "Подпись",
            T("signerPosition", "Должность"),
            T("signerFio", "ФИО"),
            T("signerInn", "ИНН подписанта", false, 12));
    }

    private static void AddAccountingSigners(DocumentDefinition d)
    {
        Section(d, "Подписи должностных лиц",
            T("signerPosition", "Должность руководителя"),
            T("signerFio", "ФИО руководителя"),
            T("chiefAccountantFio", "ФИО главного бухгалтера"));
    }

    private static void AddBilateralSigners(DocumentDefinition d, string party1 = "Исполнитель", string party2 = "Заказчик")
    {
        Section(d, $"Подпись со стороны {party1}",
            T("signer1Position", "Должность"),
            T("signer1Fio", "ФИО"),
            T("signer1Inn", "ИНН (при наличии)"));

        Section(d, $"Подпись со стороны {party2}",
            T("signer2Position", "Должность"),
            T("signer2Fio", "ФИО"),
            T("signer2Inn", "ИНН (при наличии)"));
    }

    private static List<DocumentDefinition> Build()
    {
        var list = new List<DocumentDefinition>();
        var d = default(DocumentDefinition);

        // ===================== ПРОДАЖИ =====================

        d = Doc("invoice", "Счёт на оплату", "Продажи",
            "Счёт на оплату товаров и услуг с банковскими реквизитами и условиями поставки.", "СчетНаОплату");
        AddDocumentBase(d);
        AddParties(d, "Поставщик", "Покупатель", true, true);
        Section(d, "Условия оплаты",
            D("validUntil", "Счёт действителен до"),
            A("paymentConditions", "Условия оплаты и поставки"),
            T("payPurpose", "Назначение платежа для платёжного поручения"));
        AddItems(d);
        AddVatBlock(d);
        AddAccountingSigners(d);
        list.Add(d);

        d = Doc("commercial-offer", "Коммерческое предложение", "Продажи",
            "Коммерческое предложение с описанием условий, цен и преимуществ.", "КоммерческоеПредложение");
        AddDocumentBase(d);
        AddParties(d, "Продавец", "Лицо, которому адресовано", false, false);
        Section(d, "Условия предложения",
            D("validUntil", "Предложение действительно до"),
            A("deliveryTerms", "Сроки и условия поставки"),
            A("paymentTerms", "Порядок оплаты"));
        AddItems(d);
        AddVatBlock(d);
        AddSignerBlock(d);
        list.Add(d);

        d = Doc("invoice-offer", "Счёт-договор", "Продажи",
            "Совмещённый счёт и договор-оферта на поставку товаров или оказание услуг.", "СчетДоговор");
        AddDocumentBase(d);
        AddParties(d, "Поставщик", "Покупатель", true, true);
        Section(d, "Условия договора-оферты",
            D("validUntil", "Срок акцепта (оплаты) счёта"),
            A("deliveryTerms", "Срок и порядок поставки (оказания услуг)"),
            A("paymentTerms", "Условия оплаты и перехода права собственности"));
        AddItems(d);
        AddVatBlock(d);
        AddAccountingSigners(d);
        list.Add(d);

        d = Doc("customer-order", "Заказ покупателя", "Продажи",
            "Заказ на поставку товаров или оказание услуг от покупателя.", "ЗаказПокупателя");
        AddDocumentBase(d);
        AddParties(d, "Поставщик", "Покупатель", false, true);
        Section(d, "Параметры заказа",
            D("deliveryDate", "Желаемая дата доставки"),
            T("deliveryAddress", "Адрес доставки склада"),
            A("orderComment", "Комментарий к заказу"));
        AddItems(d);
        AddVatBlock(d);
        AddSignerBlock(d);
        list.Add(d);

        list.Add(Doc("upd", "УПД (универсальный передаточный документ)", "Продажи",
            "Универсальный передаточный документ, формат XML 5.03 ФНС РФ (КНД 1115131).", "УПД"));

        d = Doc("torg12", "ТОРГ-12", "Продажи",
            "Товарная накладная ТОРГ-12 на отгрузку товаров (унифицированная форма).", "ТОРГ12");
        AddDocumentBase(d);
        AddParties(d, "Поставщик", "Плательщик", true, true);
        Section(d, "Грузоотправитель и грузополучатель",
            T("shipperName", "Грузоотправитель (наименование, адрес, ОКПО)"),
            T("consigneeName", "Грузополучатель (наименование, адрес, ОКПО)"));
        Section(d, "Транспортная накладная",
            T("waybillNo", "Номер транспортной накладной"),
            D("waybillDate", "Дата транспортной накладной"));
        Section(d, "Масса и места",
            N("grossWeight", "Масса брутто (кг)"),
            N("netWeight", "Масса нетто (кг)"),
            N("placesCount", "Количество мест"));
        AddItems(d);
        AddVatBlock(d);
        Section(d, "Доверенность на получение",
            T("attorneyNo", "Доверенность №"),
            D("attorneyDate", "Дата выдачи доверенности"),
            T("attorneyIssuedBy", "Кем выдана доверенность"),
            T("attorneyAgentFio", "Кому выдана (ФИО представителя)"));
        Section(d, "Отпуск и приёмка груза",
            T("releasedFio", "Отпуск груза произвел (должность, ФИО)"),
            T("receivedFio", "Груз принял (должность, ФИО)"),
            T("cargoReceiverFio", "Груз получил грузополучатель (должность, ФИО)"));
        AddAccountingSigners(d);
        list.Add(d);

        d = Doc("invoice-vat", "Счёт-фактура", "Продажи",
            "Счёт-фактура по форме Постановления Правительства РФ № 1137.", "СчетФактура");
        AddDocumentBase(d);
        AddParties(d, "Продавец", "Покупатель", true, true);
        Section(d, "Грузоотправитель и грузополучатель",
            T("shipperName", "Грузоотправитель и его адрес (строка 3)"),
            T("consigneeName", "Грузополучатель и его адрес (строка 4)"));
        Section(d, "Сведения об отгрузке и платежах",
            T("payDocInfo", "К платежно-расчетному документу № / дата (строка 5)"),
            T("shipmentDocInfo", "Документ об отгрузке: наим., №, дата (строка 5а)"),
            T("stateContractId", "Идентификатор госконтракта (строка 8)"));
        AddItems(d);
        AddVatBlock(d);
        AddAccountingSigners(d);
        list.Add(d);

        d = Doc("specification", "Спецификация", "Продажи",
            "Спецификация к договору поставки с согласованным перечнем товаров.", "Спецификация");
        AddDocumentBase(d);
        AddParties(d, "Поставщик", "Покупатель", false, true);
        Section(d, "Условия поставки",
            D("deliveryDate", "Срок поставки"),
            A("deliveryTerms", "Условия и адрес поставки"),
            A("paymentTerms", "Порядок оплаты"));
        AddItems(d);
        AddVatBlock(d);
        AddBilateralSigners(d, "Поставщик", "Покупатель");
        list.Add(d);

        // ===================== АКТЫ =====================

        d = Doc("act-works", "Акт выполненных работ", "Акты",
            "Акт приёмки выполненных работ или оказанных услуг с фиксацией отсутствия претензий.", "АктВыполненныхРабот");
        AddDocumentBase(d);
        AddParties(d, "Исполнитель", "Заказчик", false, true);
        Section(d, "Период выполнения работ",
            D("workStartDate", "Дата начала работ"),
            D("workEndDate", "Дата окончания работ"));
        AddItems(d, "Выполненные работы (оказанные услуги)");
        AddVatBlock(d);
        Section(d, "Претензии и гарантии",
            A("acceptanceNote", "Претензии Заказчика по объёму, качеству и срокам (при отсутствии: «Претензий нет»)", true));
        AddBilateralSigners(d, "Исполнитель", "Заказчик");
        list.Add(d);

        d = Doc("act-ks2", "КС-2 (строительство)", "Акты",
            "Акт о приёмке выполненных строительно-монтажных работ (форма КС-2).", "АктКС2");
        AddDocumentBase(d);
        AddParties(d, "Подрядчик", "Заказчик", false, true);
        Section(d, "Строительный объект",
            T("objectName", "Наименование объекта", true),
            T("objectAddress", "Адрес стройки"),
            T("investorName", "Инвестор"),
            D("periodStart", "Начало отчётного периода"),
            D("periodEnd", "Окончание отчётного периода"));
        AddItems(d, "Выполненные работы");
        AddVatBlock(d);
        AddBilateralSigners(d, "Генподрядчик", "Заказчик");
        list.Add(d);

        d = Doc("act-ks3", "КС-3 (справка)", "Акты",
            "Справка о стоимости выполненных работ и затрат (форма КС-3).", "АктКС3");
        AddDocumentBase(d);
        AddParties(d, "Подрядчик", "Заказчик", false, true);
        Section(d, "Строительный объект",
            T("objectName", "Наименование объекта", true),
            T("objectAddress", "Адрес стройки"),
            T("investorName", "Инвестор"),
            D("periodStart", "Начало отчётного периода"),
            D("periodEnd", "Окончание отчётного периода"));
        Section(d, "Итоги стоимости",
            M("totalWorks", "Стоимость выполненных работ (руб.)", true),
            M("totalVat", "В том числе НДС"),
            M("totalWithVat", "Итого с НДС (руб.)"));
        AddBilateralSigners(d, "Генподрядчик", "Заказчик");
        list.Add(d);

        d = Doc("act-reconciliation", "Акт сверки", "Акты",
            "Акт сверки взаимных расчётов между контрагентами за период.", "АктСверки");
        AddDocumentBase(d);
        AddParties(d, "Организация 1", "Организация 2", false, true);
        Section(d, "Период сверки",
            D("periodStart", "Начало периода", true),
            D("periodEnd", "Окончание периода", true));
        Section(d, "Сальдо и обороты",
            M("balanceStartDebit", "Сальдо начальное (Дебет)"),
            M("balanceStartCredit", "Сальдо начальное (Кредит)"),
            M("turnoverDebit", "Обороты за период (Дебет)"),
            M("turnoverCredit", "Обороты за период (Кредит)"),
            M("balanceEndDebit", "Сальдо конечное (Дебет)"),
            M("balanceEndCredit", "Сальдо конечное (Кредит)"));
        Section(d, "Результат сверки",
            A("reconciliationResult", "Итог сверки (задолженность в пользу одной из сторон)"));
        AddBilateralSigners(d, "Организация 1", "Организация 2");
        list.Add(d);

        d = Doc("act-writeoff", "Акт списания", "Акты",
            "Акт на списание товаров, материалов или основных средств (форма ТОРГ-16 / ОС-4).", "АктСписания");
        AddDocumentBase(d);
        Section(d, "Организация и комиссия",
            T("orgName", "Наименование организации", true),
            T("commissionHead", "Председатель комиссии (должность, ФИО)"),
            T("commissionMembers", "Члены комиссии (ФИО)"),
            T("accountablePerson", "Материально ответственное лицо (ФИО)"));
        Section(d, "Причина списания",
            A("writeoffReason", "Причина списания (износ, порча, бой, брак)", true));
        AddItems(d, "Списываемые ценности");
        Section(d, "Итоги списания",
            M("totalWriteoff", "Итого сумма списания (руб.)", true));
        AddAccountingSigners(d);
        list.Add(d);

        // ===================== ДОГОВОРЫ =====================

        d = Doc("employment-contract", "Трудовой договор", "Договоры",
            "Трудовой договор между работодателем и работником.", "ТрудовойДоговор");
        AddDocumentBase(d);
        Section(d, "Стороны договора",
            T("employerName", "Работодатель (полное наименование)", true),
            T("employerInn", "ИНН работодателя"),
            T("employeeFio", "ФИО работника", true),
            T("employeeInn", "ИНН работника", false, 12),
            T("employeePassport", "Паспортные данные работника", true));
        Section(d, "Должностные условия",
            T("position", "Должность", true),
            T("department", "Структурное подразделение"),
            D("startDate", "Дата начала работы", true),
            B("fixedTerm", "Срочный договор"),
            D("endDate", "Дата окончания срока"),
            T("schedule", "Режим работы"),
            M("salary", "Оклад (руб.)", true),
            N("probationMonths", "Испытательный срок (мес.)"));
        Section(d, "Подписи сторон",
            T("signerEmployer", "Представитель работодателя (ФИО)", true),
            T("signerEmployee", "Подпись работника (ФИО)", true));
        list.Add(d);

        d = Doc("guarantee-letter", "Гарантийное письмо", "Договоры",
            "Гарантийное письмо об исполнении денежного обязательства или поставке.", "ГарантийноеПисьмо");
        AddDocumentBase(d);
        AddParties(d, "Гарант (организация)", "Адресат", false, false);
        Section(d, "Содержание обязательства",
            A("guaranteeText", "Текст гарантии", true),
            M("guaranteeAmount", "Сумма гарантии (руб.)"),
            D("guaranteeDate", "Срок исполнения обязательства"),
            T("contractRef", "Ссылка на договор / счёт"));
        AddAccountingSigners(d);
        list.Add(d);

        d = Doc("refund-application", "Заявление на возврат денежных средств", "Договоры",
            "Заявление плательщика на возврат денежных средств с банковскими реквизитами.", "ЗаявлениеНаВозврат");
        AddDocumentBase(d);
        Section(d, "Сведения о заявителе",
            T("applicantFio", "ФИО / наименование заявителя", true),
            T("applicantInn", "ИНН"),
            T("applicantPassport", "Паспортные данные"),
            T("applicantPhone", "Контактный телефон"),
            T("applicantEmail", "Email"));
        Section(d, "Сведения о возврате",
            M("refundAmount", "Сумма к возврату (руб.)", true),
            T("refundAmountWords", "Сумма прописью"),
            A("refundReason", "Причина возврата (ошибочный платеж, расторжение)", true),
            T("orderNo", "Номер заказа / счёта"));
        Section(d, "Банковские реквизиты для перечисления",
            T("bankBik", "БИК банка"),
            T("bankName", "Наименование банка"),
            T("bankAccount", "Расчётный счёт (20 знаков)"),
            T("bankCorr", "Корр. счёт"),
            T("cardRecipientFio", "ФИО получателя карты"));
        Section(d, "Подпись заявителя",
            T("signerFio", "ФИО заявителя", true),
            D("signDate", "Дата заявления"));
        list.Add(d);

        d = Doc("contract-supply", "Договор поставки", "Договоры",
            "Договор оптовой поставки товаров с условиями приёмки и оплаты.", "ДоговорПоставки");
        AddDocumentBase(d);
        AddParties(d, "Поставщик", "Покупатель", true, false);
        Section(d, "Предмет и условия",
            A("goodsSubject", "Предмет договора (наименование товаров)", true),
            M("contractAmount", "Ориентировочная сумма договора (руб.)"),
            D("deliveryDeadline", "Срок поставки"),
            A("deliveryTerms", "Порядок и место поставки"),
            A("paymentTerms", "Порядок расчетов"));
        AddBilateralSigners(d, "Поставщик", "Покупатель");
        list.Add(d);

        d = Doc("contract-service", "Договор оказания услуг", "Договоры",
            "Договор возмездного оказания услуг с фиксацией этапов и оплаты.", "ДоговорОказанияУслуг");
        AddDocumentBase(d);
        AddParties(d, "Исполнитель", "Заказчик", true, false);
        Section(d, "Предмет и условия",
            A("serviceSubject", "Оказываемые услуги", true),
            M("serviceCost", "Стоимость услуг (руб.)", true),
            D("serviceStart", "Дата начала оказания услуг"),
            D("serviceEnd", "Срок завершения услуг"),
            A("acceptanceTerms", "Порядок приёмки услуг и подписания акта"));
        AddBilateralSigners(d, "Исполнитель", "Заказчик");
        list.Add(d);

        d = Doc("contract-self-employed", "Договор с самозанятым", "Договоры",
            "Договор гражданско-правового характера с плательщиком налога на профдоход (НПД).", "ДоговорССамозанятым");
        AddDocumentBase(d);
        Section(d, "Заказчик",
            T("customerName", "Наименование компании", true),
            T("customerInn", "ИНН"),
            T("customerKpp", "КПП"),
            T("customerAddress", "Адрес"));
        Section(d, "Исполнитель (самозанятый)",
            T("executorFio", "ФИО самозанятого", true),
            T("executorInn", "ИНН самозанятого (12 цифр)", true, 12),
            T("executorPassport", "Паспортные данные", true),
            T("executorAccount", "Номер счёта / карты для оплаты"),
            T("executorBankBik", "БИК банка"));
        Section(d, "Предмет и чек",
            A("taskScope", "Задание (описание работ / услуг)", true),
            M("rewardAmount", "Вознаграждение (руб.)", true),
            D("taskDeadline", "Срок сдачи результата"),
            B("receiptObligation", "Обязательство предоставить чек из приложения «Мой налог»"));
        AddBilateralSigners(d, "Заказчик", "Самозанятый");
        list.Add(d);

        d = Doc("contract-sale", "Договор купли-продажи товара", "Договоры",
            "Договор купли-продажи имущества или оборудования между сторонами.", "ДоговорКуплиПродажи");
        AddDocumentBase(d);
        AddParties(d, "Продавец", "Покупатель", true, false);
        Section(d, "Предмет купли-продажи",
            T("goodsSubject", "Наименование товара / объекта", true),
            N("goodsQuantity", "Количество"),
            M("goodsPrice", "Цена за единицу (руб.)"),
            M("contractAmount", "Общая стоимость договора (руб.)", true),
            D("transferDate", "Срок передачи товара"));
        AddBilateralSigners(d, "Продавец", "Покупатель");
        list.Add(d);

        d = Doc("contract-rent-commercial", "Аренда помещения (коммерческая)", "Договоры",
            "Договор аренды нежилого коммерческого помещения (офис, склад, торговая площадь).", "ДоговорАрендыПомещения");
        AddDocumentBase(d);
        AddParties(d, "Арендодатель", "Арендатор", true, false);
        Section(d, "Объект аренды",
            A("premisesAddress", "Адрес помещения", true),
            N("area", "Площадь (кв. м)", true),
            T("purpose", "Целевое назначение помещения"));
        Section(d, "Финансовые условия",
            M("rentMonthly", "Ежемесячная арендная плата (руб.)", true),
            M("deposit", "Обеспечительный (гарантийный) платёж"),
            D("rentStart", "Дата передачи помещения"),
            D("rentEnd", "Дата окончания аренды"),
            N("noticeDays", "Срок уведомления о расторжении (дней)"));
        AddBilateralSigners(d, "Арендодатель", "Арендатор");
        list.Add(d);

        d = Doc("contract-land-rent", "Аренда земли", "Договоры",
            "Договор аренды земельного участка.", "ДоговорАрендыЗемли");
        AddDocumentBase(d);
        AddParties(d, "Арендодатель", "Арендатор", true, false);
        Section(d, "Земельный участок",
            T("cadastralNo", "Кадастровый номер участка", true),
            A("landAddress", "Адрес / местоположение", true),
            N("landArea", "Площадь (кв. м)"),
            T("purpose", "Категория земель и вид разрешённого использования"));
        Section(d, "Условия аренды",
            M("rentAnnual", "Годовая арендная плата (руб.)", true),
            D("rentStart", "Дата начала аренды"),
            D("rentEnd", "Дата окончания аренды"));
        AddBilateralSigners(d, "Арендодатель", "Арендатор");
        list.Add(d);

        d = Doc("nda", "NDA (соглашение о конфиденциальности)", "Договоры",
            "Соглашение о неразглашении конфиденциальной информации и коммерческой тайны.", "NDA");
        AddDocumentBase(d);
        AddParties(d, "Раскрывающая сторона", "Получающая сторона", false, false);
        Section(d, "Предмет и срок",
            A("confidentialScope", "Предмет конфиденциальной информации (ноу-хау, код, базы)", true),
            D("ndaStart", "Дата вступления в силу"),
            D("ndaEnd", "Срок действия соглашения"),
            B("survivesTermination", "Обязательство сохраняется после расторжения (3 года)"));
        AddBilateralSigners(d, "Раскрывающая сторона", "Получающая сторона");
        list.Add(d);

        // ===================== КАССА =====================

        d = Doc("pko", "ПКО (приходный кассовый ордер)", "Касса",
            "Приходный кассовый ордер (форма КО-1) с квитанцией к ПКО.", "ПКО");
        AddDocumentBase(d);
        Section(d, "Организация",
            T("orgName", "Наименование организации", true),
            T("orgInn", "ИНН"),
            T("department", "Структурное подразделение"));
        Section(d, "Принято от",
            T("fromFio", "Принято от (ФИО / организация)", true));
        Section(d, "Сумма и основание",
            T("reason", "Основание платежа", true),
            M("amount", "Сумма (руб.)", true),
            T("amountWords", "Сумма прописью"),
            B("withNds", "В том числе НДС"),
            M("vatAmount", "Сумма НДС (руб.)"),
            T("attachment", "Приложение (документы)"));
        Section(d, "Подписи кассы",
            T("chiefAccountantFio", "Главный бухгалтер (ФИО)"),
            T("cashierFio", "Кассир (ФИО)"));
        list.Add(d);

        d = Doc("rko", "РКО (расходный кассовый ордер)", "Касса",
            "Расходный кассовый ордер (форма КО-2) на выдачу наличных денег из кассы.", "РКО");
        AddDocumentBase(d);
        Section(d, "Организация",
            T("orgName", "Наименование организации", true),
            T("orgInn", "ИНН"),
            T("department", "Структурное подразделение"));
        Section(d, "Выдать",
            T("toFio", "Выдать (ФИО получателя)", true),
            T("toPasport", "Паспортные данные получателя (серия, номер, кем и когда выдан)", true));
        Section(d, "Сумма и основание",
            T("reason", "Основание выдачи", true),
            M("amount", "Сумма (руб.)", true),
            T("amountWords", "Сумма прописью"),
            T("attachment", "Приложение"));
        Section(d, "Подписи кассы",
            T("signerPosition", "Должность руководителя"),
            T("signerFio", "Руководитель организации (ФИО)"),
            T("chiefAccountantFio", "Главный бухгалтер (ФИО)"),
            T("cashierFio", "Кассир (ФИО)"));
        list.Add(d);

        d = Doc("cashbook", "Кассовая книга", "Касса",
            "Кассовая книга (форма КО-4) за операционный день / период.", "КассоваяКнига");
        AddDocumentBase(d);
        Section(d, "Организация и период",
            T("organization", "Наименование организации", true),
            D("periodStart", "Начало периода", true),
            D("periodEnd", "Окончание периода", true));
        Section(d, "Кассовые итоги",
            M("balanceStart", "Остаток денег на начало"),
            M("incomeTotal", "Приход за период (всего)"),
            M("expenseTotal", "Расход за период (всего)"),
            M("balanceEnd", "Остаток денег на конец дня/периода"));
        AddAccountingSigners(d);
        list.Add(d);

        d = Doc("receipt-kkm", "Кассовый чек (ККМ)", "Касса",
            "Кассовый чек онлайн-кассы по 54-ФЗ с фискальными реквизитами.", "КассовыйЧек");
        AddDocumentBase(d);
        Section(d, "Продавец и ККТ",
            T("sellerName", "Наименование пользователя ККТ", true),
            T("sellerInn", "ИНН", true),
            T("kktRegNo", "Регистрационный номер ККТ", true),
            T("fnNumber", "Заводской номер ФН (16 цифр)", true),
            T("fiscalSign", "Фискальный признак документа (ФПД)", true),
            T("taxSystem", "Система налогообложения (ОСН / УСН)"));
        Section(d, "Параметры чека",
            T("cashier", "Кассир"),
            M("total", "Итого к оплате (руб.)", true));
        AddItems(d, "Позиции чека");
        list.Add(d);

        d = Doc("commodity-check", "Товарный чек", "Касса",
            "Товарный чек на товары, выданный покупателю взамен или в дополнение к чеку.", "ТоварныйЧек");
        AddDocumentBase(d);
        Section(d, "Продавец",
            T("sellerName", "Наименование продавца / ИП", true),
            T("sellerInn", "ИНН продавца"));
        AddItems(d, "Приобретённые товары");
        Section(d, "Итоги расчёта",
            M("total", "Итого сумма к оплате (руб.)", true),
            T("amountWords", "Сумма прописью"));
        AddSignerBlock(d, "Продавец");
        list.Add(d);

        // ===================== УПРАВЛЕНИЕ =====================

        d = Doc("business-card", "Карточка предприятия", "Управление",
            "Карточка предприятия с полными юридическими, налоговыми и банковскими реквизитами.", "КарточкаПредприятия");
        Section(d, "Организация",
            T("orgName", "Полное наименование организации", true),
            T("orgShortName", "Краткое наименование"),
            T("orgInn", "ИНН", true),
            T("orgKpp", "КПП"),
            T("orgOgrn", "ОГРН / ОГРНИП", true),
            T("orgOkpo", "ОКПО"),
            T("orgOkved", "Основной код ОКВЭД"));
        Section(d, "Руководство",
            T("directorPosition", "Должность руководителя"),
            T("directorFio", "ФИО руководителя"),
            T("chiefAccountantFio", "ФИО главного бухгалтера"));
        Section(d, "Адреса и контакты",
            A("orgAddress", "Юридический адрес", true),
            A("orgFactAddress", "Фактический / почтовый адрес"),
            T("orgPhone", "Телефон"),
            T("orgEmail", "Email"),
            T("orgSite", "Сайт"));
        Section(d, "Банковские реквизиты",
            T("bankName", "Наименование банка"),
            T("bankBik", "БИК"),
            T("bankAccount", "Расчётный счёт"),
            T("bankCorr", "Корр. счёт"));
        list.Add(d);

        d = Doc("vacation-application", "Заявление на отпуск", "Управление",
            "Заявление сотрудника на предоставление ежегодного оплачиваемого отпуска.", "ЗаявлениеНаОтпуск");
        AddDocumentBase(d);
        Section(d, "Заявитель и работодатель",
            T("employerName", "Наименование работодателя / организации", true),
            T("directorFio", "Кому (ФИО руководителя)"),
            T("employeeFio", "От кого (ФИО работника)", true),
            T("employeePosition", "Должность работника"),
            T("department", "Подразделение"));
        Section(d, "Сроки отпуска",
            D("vacationStart", "Дата начала отпуска", true),
            D("vacationEnd", "Дата окончания отпуска", true),
            N("vacationDays", "Количество календарных дней отпуска", true),
            T("vacationKind", "Вид отпуска (ежегодный основной / за свой счёт)"));
        Section(d, "Подпись",
            D("applicationDate", "Дата подачи заявления", true));
        list.Add(d);

        d = Doc("dismissal-application", "Заявление на увольнение", "Управление",
            "Заявление работника об увольнении по собственному желанию (п. 3 ч. 1 ст. 77 ТК РФ).", "ЗаявлениеНаУвольнение");
        AddDocumentBase(d);
        Section(d, "Заявитель и работодатель",
            T("employerName", "Наименование работодателя", true),
            T("directorFio", "Кому (ФИО руководителя)"),
            T("employeeFio", "От кого (ФИО работника)", true),
            T("employeePosition", "Должность"));
        Section(d, "Условия увольнения",
            D("dismissalDate", "Прошу уволить с (дата последнего рабочего дня)", true),
            A("dismissalReason", "Основание / формулировка заявления"),
            B("backrefund", "С компенсацией / удержанием неиспользованного отпуска"));
        Section(d, "Подпись",
            D("applicationDate", "Дата подачи заявления", true));
        list.Add(d);

        d = Doc("business-trip", "Командировочное удостоверение", "Управление",
            "Командировочное удостоверение работника (форма Т-10) с отметками о пребывании.", "Командировочное");
        AddDocumentBase(d);
        Section(d, "Работник",
            T("employerName", "Наименование организации", true),
            T("employeeFio", "ФИО командируемого работника", true),
            T("employeePosition", "Должность"),
            T("department", "Подразделение"));
        Section(d, "Командировка",
            T("destination", "Место назначения (страна, город, организация)", true),
            T("tripPurpose", "Цель служебной командировки", true),
            D("tripStart", "Дата выбытия", true),
            D("tripEnd", "Дата прибытия обратно", true),
            N("tripDays", "Количество дней командировки"));
        Section(d, "Отметки о пребывании",
            T("departureMark", "Отметка об убытии из организации"),
            T("arrivalMark", "Отметка о прибытии в пункт назначения"));
        AddSignerBlock(d);
        list.Add(d);

        d = Doc("advance-report", "Авансовый отчёт", "Управление",
            "Авансовый отчёт (форма АО-1) о командировочных и хозяйственных расходах.", "АвансовыйОтчет");
        AddDocumentBase(d);
        Section(d, "Организация и подотчётное лицо",
            T("orgName", "Наименование организации", true),
            T("accountableFio", "ФИО подотчётного лица", true),
            T("accountablePosition", "Должность"),
            T("tabNumber", "Табельный номер"),
            T("department", "Подразделение"));
        Section(d, "Суммы аванса и расходов",
            T("advancePurpose", "Назначение аванса (командировка, хоз. нужды)"),
            M("advanceIssued", "Аванс получен (руб.)", true),
            M("advanceSpent", "Израсходовано (руб.)", true),
            M("advanceBalance", "Остаток / перерасход (руб.)"),
            N("documentsCount", "Количество подтверждающих документов"));
        AddAccountingSigners(d);
        list.Add(d);

        d = Doc("power-of-attorney-m2", "Доверенность на получение товара (М-2)", "Управление",
            "Доверенность типовой формы М-2 на получение товарно-материальных ценностей.", "ДоверенностьМ2");
        AddDocumentBase(d);
        Section(d, "Срок действия доверенности",
            D("poaStart", "Дата выдачи", true),
            D("poaEnd", "Действительна до", true));
        Section(d, "Организация (плательщик)",
            T("principalName", "Организация", true),
            T("principalInn", "ИНН"),
            T("principalKpp", "КПП"),
            T("bankAccount", "Номер счёта плательщика"),
            T("bankName", "Наименование банка"));
        Section(d, "Поверенный (кому выдана)",
            T("agentFio", "ФИО поверенного", true),
            T("agentPosition", "Должность"),
            T("agentPassport", "Паспортные данные (серия, номер, кем и когда выдан)", true));
        Section(d, "Поставщик и документ",
            T("goodsSupplier", "Поставщик (наименование организации)", true),
            T("documentNo", "По наряду / счёту / договору №"));
        AddItems(d, "Перечень ценностей к получению");
        AddAccountingSigners(d);
        list.Add(d);

        d = Doc("waybill", "Путевой лист", "Управление",
            "Путевой лист автомобиля с маршрутом, пробегом и расходом топлива.", "ПутевойЛист");
        AddDocumentBase(d);
        Section(d, "Организация и водитель",
            T("orgName", "Наименование перевозчика / организации", true),
            T("driverFio", "Водитель (ФИО)", true),
            T("driverLicense", "Водительское удостоверение"));
        Section(d, "Автомобиль",
            T("vehicleModel", "Марка и модель автомобиля", true),
            T("vehiclePlate", "Государственный регистрационный номер", true));
        Section(d, "Маршрут и показатели",
            A("route", "Маршрут движения (пункты)", true),
            T("mileage", "Показания одометра при выезде/возврате"),
            T("fuelStart", "Остаток топлива при выезде (л)"),
            T("fuelEnd", "Остаток топлива при возвращении (л)"));
        AddSignerBlock(d, "Диспетчер / механик");
        list.Add(d);

        d = Doc("work-order", "Заказ-наряд", "Управление",
            "Заказ-наряд на выполнение ремонтных, сервисных или монтажных работ.", "ЗаказНаряд");
        AddDocumentBase(d);
        Section(d, "Исполнитель и заказчик",
            T("contractorName", "Исполнитель (автосервис / мастерская)", true),
            T("customerName", "Заказчик (ФИО / организация)", true),
            T("customerPhone", "Телефон заказчика"),
            T("vehicleOrObject", "Объект ремонта / автомобиль (марка, госномер)"));
        Section(d, "Работы и материалы",
            A("workDescription", "Описание необходимых работ", true),
            D("workStart", "Дата начала работ"),
            D("workEnd", "Срок завершения"),
            M("workCost", "Стоимость работ (руб.)"),
            M("partsCost", "Стоимость запасных частей / материалов (руб.)"),
            M("totalCost", "Итого к оплате (руб.)", true));
        AddBilateralSigners(d, "Исполнитель", "Заказчик");
        list.Add(d);

        d = Doc("transport-invoice", "Транспортная накладная (ЭТрН)", "Управление",
            "Электронная транспортная накладная (ЭТрН) по формату ФНС России.", "ЭТрН");
        AddDocumentBase(d);
        Section(d, "Грузоотправитель",
            T("shipperName", "Наименование грузоотправителя", true),
            T("shipperInn", "ИНН грузоотправителя"),
            A("shipperAddress", "Адрес погрузки"));
        Section(d, "Грузополучатель",
            T("consigneeName", "Наименование грузополучателя", true),
            T("consigneeInn", "ИНН грузополучателя"),
            A("consigneeAddress", "Адрес выгрузки"));
        Section(d, "Перевозчик",
            T("carrierName", "Наименование перевозчика", true),
            T("carrierInn", "ИНН перевозчика"),
            T("driverFio", "Водитель (ФИО)"),
            T("vehicleModel", "Марка и модель тягача / прицепа"),
            T("vehiclePlate", "Госномер автомобиля"));
        Section(d, "Груз",
            T("cargoDescription", "Наименование груза", true),
            N("cargoWeight", "Масса груза (т/кг)"),
            N("cargoPlaces", "Количество грузовых мест"),
            D("loadingDate", "Дата и время погрузки"),
            D("unloadingDate", "Дата доставки"));
        AddSignerBlock(d);
        list.Add(d);

        d = Doc("transport-order", "Электронный заказ-заявка (ЭЗЗ)", "Управление",
            "Электронная заказ-заявка на организацию перевозки груза автотранспортом.", "ЭЗЗ");
        AddDocumentBase(d);
        Section(d, "Стороны",
            T("customerName", "Заказчик перевозки", true),
            T("customerInn", "ИНН заказчика"),
            T("carrierName", "Перевозчик / экспедитор", true),
            T("carrierInn", "ИНН перевозчика"));
        Section(d, "Условия рейса",
            T("cargoDescription", "Наименование и характер груза", true),
            A("route", "Маршрут перевозки (погрузка — выгрузка)", true),
            D("pickupDate", "Дата и время подачи транспорта"),
            T("vehicleType", "Требуемый тип подвижного состава (тент, реф, борт)"),
            M("cost", "Стоимость перевозки (руб.)", true),
            A("paymentTerms", "Порядок оплаты рейса"));
        AddBilateralSigners(d, "Заказчик", "Перевозчик");
        list.Add(d);

        d = Doc("ttn", "ТТН (товарно-транспортная накладная)", "Управление",
            "Товарно-транспортная накладная (форма 1-Т) с товарным и транспортным разделами.", "ТТН");
        AddDocumentBase(d);
        Section(d, "Участники перевозки",
            T("shipperName", "Грузоотправитель", true),
            T("consigneeName", "Грузополучатель", true),
            T("carrierName", "Перевозчик", true),
            T("payerName", "Заказчик (плательщик)"));
        Section(d, "Транспорт и груз",
            T("vehicleModel", "Автомобиль (марка, госномер)"),
            T("driverFio", "ФИО водителя"),
            T("cargoDescription", "Наименование груза", true),
            N("cargoWeight", "Вес груза (кг)"),
            T("cargoPlaces", "Количество мест"),
            A("route", "Маршрут перевозки"));
        AddItems(d, "Товарный раздел (наименования и стоимость)");
        AddSignerBlock(d);
        list.Add(d);

        // ===================== ПЛАТЕЖИ =====================

        d = Doc("payment-order", "Платёжное поручение", "Платежи",
            "Платёжное поручение (форма 0401060) в соответствии с Положением Банка России № 762-П.", "ПлатежноеПоручение");
        AddDocumentBase(d);
        Section(d, "Параметры платежа",
            T("payType", "Вид платежа (электронно / срочно / почтой)"),
            M("payAmount", "Сумма платежа (руб.)", true),
            T("amountWords", "Сумма прописью"),
            T("payUrgently", "Очерёдность платежа (1–5)"),
            T("payPurpose", "Назначение платежа (с указанием НДС)", true));
        Section(d, "Плательщик",
            T("payerName", "Наименование плательщика", true),
            T("payerInn", "ИНН плательщика", true),
            T("payerKpp", "КПП плательщика"),
            T("payerAccount", "Счёт плательщика (20 знаков)", true),
            T("payerBankBik", "БИК банка плательщика", true),
            T("payerBankName", "Банк плательщика"),
            T("payerBankCorr", "Корр. счёт банка плательщика"));
        Section(d, "Получатель",
            T("receiverName", "Наименование получателя", true),
            T("receiverInn", "ИНН получателя", true),
            T("receiverKpp", "КПП получателя"),
            T("receiverAccount", "Счёт получателя (20 знаков)", true),
            T("receiverBankBik", "БИК банка получателя", true),
            T("receiverBankName", "Банк получателя"),
            T("receiverBankCorr", "Корр. счёт банка получателя"));
        Section(d, "Бюджетные реквизиты (для налогов и пошлин)",
            T("payerStatus", "Статус плательщика (поле 101: 01, 02, 13)"),
            T("kbk", "КБК — 20 знаков (поле 104)"),
            T("oktmo", "ОКТМО — 8 или 11 знаков (поле 105)"),
            T("taxBase", "Основание платежа (поле 106: ТП, ЗД, 0)"),
            T("taxPeriod", "Налоговый период (поле 107: МС.01.2026, 0)"),
            T("taxDocNo", "Номер налогового документа (поле 108)"),
            D("taxDocDate", "Дата налогового документа (поле 109)"),
            T("uin", "УИН / Код (поле 22: 20-25 знаков или 0)"));
        AddAccountingSigners(d);
        list.Add(d);

        d = Doc("pd4", "Квитанция ПД-4", "Платежи",
            "Платёжная квитанция формы ПД-4 для оплаты налогов, пошлин и услуг через банк.", "ПД4");
        AddDocumentBase(d);
        Section(d, "Плательщик",
            T("payerFio", "ФИО плательщика", true),
            T("payerInn", "ИНН плательщика"),
            A("payerAddress", "Адрес плательщика"));
        Section(d, "Получатель и реквизиты",
            T("receiverName", "Наименование получателя платежа", true),
            T("receiverInn", "ИНН получателя", true),
            T("receiverKpp", "КПП получателя"),
            T("bankAccount", "Номер счёта получателя (20 знаков)", true),
            T("bankName", "Наименование банка"),
            T("bankBik", "БИК банка", true),
            T("bankCorr", "Корр. счёт"));
        Section(d, "Назначение платежа",
            A("purpose", "Наименование платежа / назначение", true),
            M("amount", "Сумма платежа (руб.)", true),
            T("oktmo", "ОКТМО (поле 105)"),
            T("kbk", "КБК (поле 104)"));
        list.Add(d);

        d = Doc("usn", "Декларация по УСН", "Платежи",
            "Налоговая декларация по УСН (доходы или доходы минус расходы).", "ДекларацияУСН");
        AddDocumentBase(d);
        Section(d, "Налогоплательщик",
            T("taxpayerName", "Наименование организации / ФИО ИП", true),
            T("taxpayerInn", "ИНН", true),
            T("taxpayerKpp", "КПП"),
            T("taxAuthorityCode", "Код налогового органа (ИФНС)", true));
        Section(d, "Налоговый период и объект",
            T("period", "Налоговый период (год)", true),
            T("objectTaxation", "Объект налогообложения (1 — доходы, 2 — доходы минус расходы)", true));
        Section(d, "Показатели",
            M("income", "Сумма полученных доходов за год (руб.)", true),
            M("expenses", "Сумма произведённых расходов (руб.)"),
            M("taxBase", "Налоговая база для исчисления налога (руб.)"),
            M("taxRate", "Налоговая ставка (%)", true),
            M("taxAmount", "Исчисленная сумма налога (руб.)", true),
            M("insurancePaid", "Уплаченные страховые взносы, уменьшающие налог"));
        AddSignerBlock(d);
        list.Add(d);

        // ===================== РЕГИСТРАЦИЯ И НАЛОГИ =====================

        d = Doc("ip-registration", "Регистрация ИП (Р21001)", "Регистрация и налоги",
            "Форма Р21001 — заявление о государственной регистрации физического лица в качестве ИП.", "Р21001");
        Section(d, "Сведения о гражданине",
            T("applicantFio", "Фамилия, имя, отчество", true),
            T("applicantInn", "ИНН физического лица", true, 12),
            T("applicantPassport", "Паспорт: серия, номер, кем и когда выдан, код подразделения", true),
            T("applicantAddress", "Адрес места жительства (по паспорту)", true),
            T("applicantPhone", "Номер телефона", true),
            T("applicantEmail", "Адрес электронной почты", true));
        Section(d, "Виды экономической деятельности",
            T("activityCode", "Основной код по ОКВЭД (не менее 4 знаков)", true),
            A("activityDesc", "Дополнительные коды по ОКВЭД"),
            B("simplifiedTax", "Приложить уведомление о переходе на УСН"));
        Section(d, "Подпись заявителя",
            D("signDate", "Дата подачи заявления", true));
        list.Add(d);

        d = Doc("ip-closure", "Закрытие ИП (Р26001)", "Регистрация и налоги",
            "Форма Р26001 — заявление о прекращении физическим лицом деятельности в качестве ИП.", "Р26001");
        Section(d, "Сведения об ИП",
            T("applicantFio", "ФИО индивидуального предпринимателя", true),
            T("applicantInn", "ИНН", true, 12),
            T("applicantOgrnip", "ОГРНИП (15 знаков)", true, 15),
            T("applicantPhone", "Контактный телефон", true),
            T("applicantEmail", "Email для направления документов", true));
        Section(d, "Подпись",
            D("signDate", "Дата подписания заявления", true));
        list.Add(d);

        d = Doc("kfh-registration", "Регистрация КФХ", "Регистрация и налоги",
            "Заявление о государственной регистрации крестьянского (фермерского) хозяйства.", "КФХ");
        Section(d, "Глава КФХ",
            T("headFio", "ФИО главы хозяйства", true),
            T("headInn", "ИНН", true, 12),
            T("headPassport", "Паспортные данные главы КФХ", true),
            A("headAddress", "Адрес места жительства"));
        Section(d, "Хозяйство",
            T("kfhName", "Наименование КФХ", true),
            T("activityCode", "Код ОКВЭД (сельскохозяйственная деятельность)", true),
            A("activityDesc", "Виды сельскохозяйственного производства"),
            D("creationDate", "Дата создания хозяйства"));
        Section(d, "Подпись",
            D("signDate", "Дата подачи"));
        list.Add(d);

        d = Doc("usn-notification", "Переход на УСН (уведомление)", "Регистрация и налоги",
            "Уведомление по форме 26.2-1 о переходе на упрощённую систему налогообложения.", "УведомлениеУСН");
        AddDocumentBase(d);
        Section(d, "Налогоплательщик",
            T("taxpayerName", "Наименование организации / ФИО ИП", true),
            T("taxpayerInn", "ИНН", true),
            T("taxpayerKpp", "КПП"),
            T("taxAuthorityCode", "Код налогового органа (ИФНС)", true));
        Section(d, "Параметры перехода",
            T("objectTaxation", "Объект налогообложения: 1 — Доходы, 2 — Доходы минус расходы", true),
            D("transitionDate", "Дата перехода на УСН (с 1 января года)", true),
            M("income9Months", "Доходы за 9 месяцев (для организаций)"));
        AddSignerBlock(d);
        list.Add(d);

        d = Doc("account-notification", "Уведомление о счёте", "Регистрация и налоги",
            "Сообщение об открытии (закрытии) банковского счёта для контролирующих органов.", "УведомлениеОСчете");
        AddDocumentBase(d);
        Section(d, "Налогоплательщик",
            T("taxpayerName", "Наименование организации / ФИО ИП", true),
            T("taxpayerInn", "ИНН", true),
            T("taxpayerKpp", "КПП"));
        Section(d, "Сведения о счёте",
            T("accountNo", "Номер банковского счёта (20 знаков)", true),
            T("bankBik", "БИК банка", true),
            T("bankName", "Наименование банка"),
            T("eventType", "Тип события (открытие / закрытие / изменение реквизитов)", true),
            D("eventDate", "Дата открытия / закрытия счёта", true));
        AddAccountingSigners(d);
        list.Add(d);

        // ===================== ПОЧТА =====================

        d = Doc("mail-inventory-107", "Опись вложения (ф. 107)", "Почта",
            "Опись вложения в ценное почтовое отправление по форме 107 Почты России.", "ОписьВложения107");
        AddDocumentBase(d);
        Section(d, "Отправитель",
            T("senderName", "Отправитель (ФИО / наименование организации)", true),
            A("senderAddress", "Почтовый адрес отправителя", true));
        Section(d, "Получатель",
            T("receiverName", "Получатель (ФИО / наименование организации)", true),
            A("receiverAddress", "Почтовый адрес получателя", true));
        Section(d, "Параметры отправления",
            T("postalType", "Вид отправления (ценное письмо / посылка / бандероль)"),
            N("itemCount", "Количество предметов во вложении"),
            M("declaredValue", "Общая объявленная ценность (руб.)", true),
            T("itemsTable", "Перечень вложений — см. таблицу ниже"),
            D("sendDate", "Дата отправки"));
        Section(d, "Подписи почты",
            T("senderFio", "Подпись отправителя (ФИО)"),
            T("postalWorkerFio", "Проверил и принял работник связи (ФИО)"));
        list.Add(d);

        d = Doc("mail-notification-119", "Уведомление о вручении (ф. 119)", "Почта",
            "Бланк уведомления о вручении почтового отправления (форма 119).", "УведомлениеОВручении119");
        AddDocumentBase(d);
        Section(d, "Отправитель (куда вернуть уведомление)",
            T("senderName", "Кому вернуть (ФИО / наименование)", true),
            A("senderAddress", "Куда вернуть (почтовый адрес)", true));
        Section(d, "Получатель отправления",
            T("receiverName", "Получатель (ФИО / организация)", true),
            A("receiverAddress", "Адрес доставки", true));
        Section(d, "Отправление",
            T("postalType", "Вид почтового отправления (заказное, ценное)"),
            T("postalIndex", "Почтовый индекс места назначения"),
            D("sendDate", "Дата отправки"),
            T("postalId", "Штриховой почтовый идентификатор (ШПИ 14 знаков)"));
        Section(d, "Вручение",
            D("receiveDate", "Дата вручения адресату"),
            T("receiverSignature", "ФИО и подпись лица, получившего отправление"));
        list.Add(d);

        d = Doc("envelope-address", "Адрес на конверте", "Почта",
            "Оформление почтового конверта по стандарту Почты России (ГОСТ Р 51506-99).", "АдресНаКонверте");
        Section(d, "Откуда (отправитель)",
            T("senderName", "От кого (ФИО / наименование организации)", true),
            T("senderStreet", "Улица, дом, корпус, квартира / офис", true),
            T("senderCity", "Город / населённый пункт, район, регион", true),
            T("senderIndex", "Почтовый индекс (6 цифр)", true));
        Section(d, "Куда (получатель)",
            T("receiverName", "Кому (ФИО / наименование организации)", true),
            T("receiverStreet", "Улица, дом, корпус, квартира / офис", true),
            T("receiverCity", "Город / населённый пункт, район, регион", true),
            T("receiverIndex", "Почтовый индекс (6 цифр)", true));
        Section(d, "Конверт и стиль",
            T("envelopeSize", "Формат конверта (С6, С5, С4, DL)", false),
            T("logoUrl", "Ссылка на логотип компании (необязательно)"));
        list.Add(d);

        return list;
    }
}
