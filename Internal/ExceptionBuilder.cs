using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonUnitSimplifier
{
    /// <summary>
    /// Помогает частично автоматически генерировать тексты исключений
    /// </summary>
    internal class ExceptionBuilder
    {
        /// <summary>
        /// Выбросит исключение с сообщением, при этом сохранит данные от предыдущего,
        /// оставит всю возможную инфу для дебага
        /// </summary>
        internal static void ThrowWithFullInfo(string append_message, Exception ex)
        {
            throw new Exception(
                $"! {append_message} !\n" +
                $"{ex.GetType().Name} - {ex.Message}\n" +
                $"Stack trace: {ex.StackTrace}\n" +
                $"Inner [\n" +
                $"{ex.InnerException?.GetType().Name} - {ex.InnerException?.Message}\n" +
                $"{ex.InnerException?.StackTrace}\n" +
                $"]\n\n");
        }
    }
}
