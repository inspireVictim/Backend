using YessBackend.Application.Enums;

namespace YessBackend.Infrastructure.Helpers;

/// <summary>
/// Вспомогательный класс для работы с кодами ошибок Optima Payment
/// Соответствует требованиям QIWI OSMP v1.4
/// </summary>
public static class OptimaErrorHelper
{
    /// <summary>
    /// Определяет, является ли ошибка фатальной
    /// Фатальная ошибка означает, что повторная отправка запроса с теми же параметрами
    /// приведет к 100% повторению той же ошибки
    /// </summary>
    /// <param name="resultCode">Код результата</param>
    /// <returns>True если ошибка фатальная, false если нефатальная</returns>
    public static bool IsFatalError(OptimaResultCode resultCode)
    {
        return resultCode switch
        {
            // Успешная операция - не ошибка
            OptimaResultCode.Ok => false,
            
            // Нефатальные ошибки - система будет повторять запрос
            OptimaResultCode.TemporaryError => false,      // 1 - Временная ошибка
            OptimaResultCode.PaymentNotCompleted => false, // 90 - Проведение платежа не окончено
            
            // Фатальные ошибки - система прекращает обработку
            OptimaResultCode.InvalidAccountFormat => true,  // 4 - Неверный формат идентификатора
            OptimaResultCode.AccountNotFound => true,       // 5 - Идентификатор не найден
            OptimaResultCode.PaymentForbidden => true,      // 7 - Прием платежа запрещен
            OptimaResultCode.AccountNotActive => true,       // 79 - Счет не активен
            OptimaResultCode.AmountTooSmall => true,        // 241 - Сумма слишком мала
            OptimaResultCode.AmountTooLarge => true,        // 242 - Сумма слишком велика
            OptimaResultCode.CannotCheckAccount => true,    // 243 - Невозможно проверить состояние счета
            OptimaResultCode.OtherError => true,            // 300 - Другая ошибка поставщика
            
            _ => true // По умолчанию считаем фатальной
        };
    }
    
    /// <summary>
    /// Получает описание кода ошибки
    /// </summary>
    public static string GetErrorDescription(OptimaResultCode resultCode)
    {
        return resultCode switch
        {
            OptimaResultCode.Ok => "OK",
            OptimaResultCode.TemporaryError => "Временная ошибка. Повторите запрос позже",
            OptimaResultCode.InvalidAccountFormat => "Неверный формат идентификатора абонента",
            OptimaResultCode.AccountNotFound => "Идентификатор абонента не найден (Ошиблись номером)",
            OptimaResultCode.PaymentForbidden => "Прием платежа запрещен поставщиком",
            OptimaResultCode.AccountNotActive => "Счет абонента не активен",
            OptimaResultCode.PaymentNotCompleted => "Проведение платежа не окончено",
            OptimaResultCode.AmountTooSmall => "Сумма слишком мала",
            OptimaResultCode.AmountTooLarge => "Сумма слишком велика",
            OptimaResultCode.CannotCheckAccount => "Невозможно проверить состояние счета",
            OptimaResultCode.OtherError => "Другая ошибка поставщика",
            _ => "Неизвестная ошибка"
        };
    }
}

