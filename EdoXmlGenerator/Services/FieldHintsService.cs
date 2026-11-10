using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using EdoXmlGenerator.Models.Catalog;

namespace EdoXmlGenerator.Services;

/// <summary>
/// Подсказки (placeholder) и правила inline-валидации для полей динамических форм.
/// Ключи и подписи полей сверяются по маскам: ИНН, КПП, ОКПО, ОКТМО, ОКАТО, ОГРН,
/// БИК, счёт, корр. счёт, индекс, телефон, e-mail, ФИО, наименование, адрес, и т.п.
/// Гарантирует наличие понятного и полезного примера для каждого поля ввода.
/// </summary>
public static class FieldHintsService
{
    public static string GetPlaceholder(FieldDef def)
    {
        if (!string.IsNullOrWhiteSpace(def.Placeholder))
            return def.Placeholder;

        var key = Normalize(def.Key);
        var label = Normalize(def.Label);

        if (def.Kind == FieldKind.Money) return "пример: 1250.50";
        if (def.Kind == FieldKind.Number) return "пример: 5";
        if (def.Kind == FieldKind.Date) return "пример: ДД.ММ.ГГГГ";
        if (def.Kind == FieldKind.Bool) return "Отметьте при необходимости (Да/Нет)";

        // Коды и классификаторы
        if (key.StartsWith("inn") || label.Contains("инн")) return "пример: 7701234567";
        if (key.StartsWith("kpp") || label.Contains("кпп")) return "пример: 770101001";
        if (key.StartsWith("okpo") || label.Contains("окпо")) return "пример: 12345678";
        if (key.StartsWith("oktmo") || label.Contains("октмо")) return "пример: 45382000";
        if (key.StartsWith("okato") || label.Contains("окато")) return "пример: 45293504000";
        if (key.StartsWith("ogrn") || label.Contains("огрнип") || label.Contains("огрн")) return "пример: 1027700132195";
        if (key.StartsWith("bik") || label.StartsWith("бик") || key.StartsWith("bankbik") || label.Contains("бик")) return "пример: 044525225";
        if (key.StartsWith("account") || key.StartsWith("corr") || label.Contains("счет") || label.Contains("счёт") ||
            label.Contains("строка 20") || (label.Contains("счет") && label.Contains("банк"))) return "пример: 40702810000000000000";
        if (key.Contains("index") || label.Contains("индекс")) return "пример: 123456";
        if (key.Contains("innkpp")) return "пример: 7701234567770101001";
        if (key.Contains("email") || label.Contains("e-mail") || label.Contains("emai")) return "пример: company@mail.ru";
        if (key.Contains("phone") || label.Contains("телефон") || label.Contains("тел.")) return "пример: +7 495 123-45-67";
        if (key.Contains("url") || key.Contains("logo") || label.Contains("сайт") || label.Contains("url")) return "пример: https://site.ru/logo.png";

        // Персоналии, ФИО, должности, паспорт
        if (key.Contains("fio") || label.Contains("фио") || label.Contains("руководител")
            || label.Contains("представител") || label.Contains("директор") || label.Contains("работник")
            || label.Contains("сотрудник") || label.Contains("глава") || label.Contains("водите")
            || label.Contains("кассир") || label.Contains("исполнител") || label.Contains("плательщик ф")
            || label.Contains("подписант") || label.Contains("получатель") && !label.Contains("банк"))
            return "пример: Иванов Иван Иванович";

        if (key.Contains("position") || label.Contains("должность")) return "пример: Генеральный директор";
        if (key.Contains("passport") || label.Contains("паспорт")) return "пример: 45 07 123456";
        if (key.Contains("snils") || label.Contains("снилс")) return "пример: 123-456-789 01";
        if (key.Contains("driverlicense") || label.Contains("водительск") || label.Contains("удостоверен"))
            return "пример: 77 12 345678";
        if (key.Contains("todocument") || label.Contains("документ, удостоверяющий")) return "пример: Паспорт гражданина РФ";

        // Бюджетные реквизиты, налоги, ККТ
        if (key.Contains("kbk") || label.Contains("кбк")) return "пример: 18210501011011000110";
        if (key.Contains("cadastr") || label.Contains("кадастров")) return "пример: 77:01:0000000:1";
        if (key.Contains("okved") || label.Contains("оквэд")) return "пример: 62.01";
        if (key.Contains("kkt") || label.Contains("фискальн") || key.Contains("fnnumber") || label.Contains("ккт") || label.Contains("фн"))
            return "пример: 0000000001234567";
        if (key.Contains("fiscalsign") || label.Contains("фискальный признак")) return "пример: 1234567890";
        if (key.Contains("objecttaxation") || label.Contains("объект налогообложения")) return "пример: Доходы (6%)";
        if (key.Contains("period") || label.Contains("налоговый период")) return "пример: 2026 год";
        if (key.Contains("vatrate") || label.Contains("ставка ндс")) return "пример: 20%";

        // Номера документов, даты, основание
        if (key.Contains("documentno") || label.Contains("номер документа") || label.Contains("номер договора")
            || (label.Contains("номер счета") && label.Contains("платеж")) || label.Contains("платежн документ")
            || label.Contains("номер плат") || key.Contains("paydocument")) return "пример: № 152";
        if (key.Contains("contractno") || label.Contains("номер договора")) return "пример: № 15/2026";
        if (key.Contains("paycode") || label.Contains("код по 20-значной")) return "пример: 0";
        if (key.Contains("payoperationno") || label.Contains("операци")) return "пример: 01";
        if (key.Contains("payurgently") || label.Contains("очерёдность") || label.Contains("очередность")) return "пример: 5";
        if (key.Contains("paypurpose") || label.Contains("назначение платежа")) return "пример: Оплата по счету № 15 за товары. В т.ч. НДС 20%";

        // Суммы прописью, текст гарантий, условий
        if (key.Contains("amountwords") || label.Contains("прописью")) return "пример: Одна тысяча двести рублей 50 копеек";
        if (key.Contains("guaranteetext") || label.Contains("гаранти")) return "пример: Обязуемся оплатить счет № 15 в срок до 25.10.2026";
        if (key.Contains("confidentialscope") || label.Contains("конфиденциальн")) return "пример: Исходный код программного обеспечения, базы данных";
        if (key.Contains("deliveryterms") || label.Contains("условия поставки")) return "пример: Доставка силами поставщика до склада покупателя";
        if (key.Contains("paymentterms") || label.Contains("условия оплаты")) return "пример: 100% предоплата в течение 3 банковских дней";
        if (key.Contains("writeoffreason") || label.Contains("списани")) return "пример: Моральный и физический износ, непригодность к ремонту";
        if (key.Contains("dismissalreason") || label.Contains("увольнен")) return "пример: По соглашению сторон, п. 1 ч. 1 ст. 77 ТК РФ";
        if (key.Contains("trippurpose") || label.Contains("командировк") || label.Contains("цель")) return "пример: Проведение деловых переговоров с партнёрами";
        if (key.Contains("reason") || label.Contains("основание")) return "пример: Служебная записка от 10.02.2026 № 4";
        if (key.Contains("purpose") || label.Contains("назначение")) return "пример: Для ведения основной уставной деятельности";

        // Позиции, товары, услуги, работы
        if (key.Contains("goodssubject") || label.Contains("предмет договора (товар)") || label.Contains("товар")) return "пример: Поставка компьютерного оборудования";
        if (key.Contains("servicesubject") || label.Contains("услуг")) return "пример: Консультационные услуги по бухгалтерскому учету";
        if (key.Contains("workdescription") || label.Contains("описание работ")) return "пример: Монтаж кабельных линий и электрооборудования";
        if (key.Contains("goodslist") || label.Contains("ценностей")) return "пример: Ноутбук Lenovo ThinkPad (сер. № 12345)";
        if (key.Contains("cargoplaces") || label.Contains("количество мест")) return "пример: 10 мест (коробок)";
        if (key.Contains("quantity") || key.Contains("itemcount") || label.Contains("количество")) return "пример: 5";
        if (key.Contains("itemstable") || label.Contains("таблиц")) return "пример: см. перечень позиций в таблице ниже";

        // Транспорт, маршрут, топливо
        if (key.Contains("vehicleplate") || label.Contains("гос. номер") || label.Contains("госномер")) return "пример: А123ВС777";
        if (key.Contains("vehiclemodel") || label.Contains("марка") || label.Contains("модель")) return "пример: ГАЗель NEXT / КамАЗ 5490";
        if (key.Contains("mileage") || label.Contains("пробег") || label.Contains("одометр")) return "пример: 125400 км";
        if (key.Contains("fuel") || label.Contains("топлив")) return "пример: 45 л";
        if (key.Contains("route") || label.Contains("маршрут")) return "пример: г. Москва, склад 1 — г. Тверь, склад 2";
        if (key.Contains("departuremark") || label.Contains("убыти")) return "пример: Убыл 15.03.2026 в 08:30";
        if (key.Contains("arrivalmark") || label.Contains("прибыти")) return "пример: Прибыл 15.03.2026 в 14:15";

        // Почта, конверты
        if (key.Contains("envelope") || label.Contains("конверт")) return "пример: С5 (162×229 мм) / DL (110×220 мм)";
        if (key.Contains("postaltype") || label.Contains("отправлени")) return "пример: Заказное письмо с уведомлением";
        if (key.Contains("postalid") || label.Contains("идентификатор отправления")) return "пример: 12700012345678";
        if (key.Contains("documentslist") || key.Contains("attachment") || label.Contains("приложен")) return "пример: Акт сверки на 2 л. в 2 экз.";

        // Адреса, города, организация
        if (label.Contains("наименование") || label.Contains("организация") || label.Contains("компания")
            || label.Contains("работодатель") || label.Contains("кфх") || label.Contains("инвестор")
            || key.Contains("name") || label.Contains("сторона") || label.Contains("арендатор")
            || label.Contains("арендодатель") || label.Contains("поставщик") || label.Contains("покупател")
            || label.Contains("заказчик") || label.Contains("подрядчик") || label.Contains("исполнител")
            || label.Contains("гарант") || label.Contains("адресат"))
            return "пример: ООО «Ромашка»";

        if (key.Contains("city") || label.Contains("город") || label.Contains("пункт")) return "пример: г. Москва";
        if (key.Contains("street") || label.Contains("улица")) return "пример: ул. Ленина, д. 10, оф. 25";
        if (key.Contains("landaddress") || label.Contains("адрес") || label.Contains("местоположение") || key.Contains("banklocation"))
            return "пример: г. Москва, ул. Ленина, д. 1";

        if (key.Contains("department") || label.Contains("подразделение")) return "пример: Отдел продаж";
        if (key.Contains("schedule") || label.Contains("режим работы")) return "пример: 5/2, с 9:00 до 18:00";
        if (key.Contains("regauthority") || label.Contains("зарегистрировано")) return "пример: МИФНС России № 46 по г. Москве";
        if (key.Contains("registrdate") || label.Contains("дата регистрации")) return "пример: 15.01.2024";

        // Универсальный осмысленный плейсхолдер для любого другого текстового поля
        if (def.Kind == FieldKind.TextArea) return "пример: Введите подробное описание или условия документа...";
        return $"пример: Введите {def.Label.ToLowerInvariant()}";
    }

    /// <summary>Возвращает текст ошибки формата или null, если значение корректно/не требует проверки.</summary>
    public static string? Validate(FieldDef def, string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        if (def.Kind == FieldKind.Number || def.Kind == FieldKind.Money)
        {
            if (!decimal.TryParse(value.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                return def.Kind == FieldKind.Money ? "Введите число (пример: 1250.50)" : "Введите число";
            return null;
        }

        var key = Normalize(def.Key);
        var label = Normalize(def.Label);
        var v = value.Trim();

        if (key.StartsWith("inn") || label.Contains("инн"))
        {
            if (!Regex.IsMatch(v, @"^\d{10}$") && !Regex.IsMatch(v, @"^\d{12}$"))
                return "ИНН: 10 или 12 цифр";
        }
        else if (key.StartsWith("kpp") || label.Contains("кпп"))
        {
            if (!Regex.IsMatch(v, @"^\d{9}$"))
                return "КПП: 9 цифр";
        }
        else if (key.StartsWith("okpo") || label.Contains("окпо"))
        {
            if (!Regex.IsMatch(v, @"^\d{8}$") && !Regex.IsMatch(v, @"^\d{10}$"))
                return "ОКПО: 8 или 10 цифр";
        }
        else if (key.StartsWith("bik") || label.StartsWith("бик") || key.StartsWith("bankbik"))
        {
            if (!Regex.IsMatch(v, @"^\d{9}$"))
                return "БИК: 9 цифр";
        }
        else if (key.StartsWith("account") || key.StartsWith("corr") || label.Contains("счет") || label.Contains("счёт"))
        {
            if (v.Length != 20 || !v.All(char.IsDigit))
                return "Счёт: 20 цифр";
        }
        else if (key.Contains("index") || label.Contains("индекс"))
        {
            if (!Regex.IsMatch(v, @"^\d{6}$"))
                return "Индекс: 6 цифр";
        }
        else if (key.StartsWith("ogrn") || label.Contains("огрн") || label.Contains("огрнип"))
        {
            if (!Regex.IsMatch(v, @"^\d{13}$") && !Regex.IsMatch(v, @"^\d{15}$"))
                return "ОГРН: 13 цифр (ЮЛ) или 15 цифр (ИП)";
        }
        else if (key.Contains("email") || label.Contains("e-mail") || label.Contains("emai"))
        {
            if (!Regex.IsMatch(v, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                return "Некорректный e-mail (пример: name@domain.ru)";
        }
        else if (key.Contains("snils") || label.Contains("снилс"))
        {
            var clean = Regex.Replace(v, @"[^\d]", "");
            if (clean.Length != 11)
                return "СНИЛС: 11 цифр";
        }

        return null;
    }

    private static string Normalize(string s) =>
        Regex.Replace(s ?? string.Empty, @"[\s\-_\.:/\\\(\)]+", "").ToLowerInvariant();
}
