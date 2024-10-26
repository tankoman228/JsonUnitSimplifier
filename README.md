JsonUnitSimplifier - это библиотека для оптимизации процесса написания юнит-тестов на C#. 

Позволяет:
- описывать весь юнит-тест в JSON-файле
- автоматически генерировать датасет по заданным правилам
- автоматически проводить проверки


# Содержание:

1. С чего начать
1.1. Правила генерации
1.2. Функции
1.3. Правила проверок
1.4. Пример тестирования функции CheckEmail
1.5. Примечания, возможные ошибки
2. Тестирование слоистой архитектуры
2.1. Определение
2.2. Цели проверок
2.3. Пример тестирования слоистой архитектуры TestedService-TestedObject
3. Генерация датасета без использования JSON
4. Полностью автоматизированное тестирование
4.1. Указатели на классы



# 1. С чего начать

Библиотека использует зависимость Newtonsoft.JSON
Библиотека однозначно совместима с платформами «.NET 8» и «.NET Framework 4.6.1»
Подключите файлы библиотеки .dll к проекту, добавив в зависимости вашего проекта для тестирования


Далее в этой главе описывается пример написания юнит-теста при помощи JsonUnitSimplifier.

// код на C#, описывающий тест
TestByJSON.TestObject<YourTestedObjectClass>(json, o => {
    // Ваша доп. логика для каждого YourTestedObjectClass o из будущего датасета        
});

В аргументе json укажите либо путь к файлу, либо саму JSON строку.

Далее создайте JSON файл для описания модульного теста. В самом простом случае файл начинается следующим образом:

{
  "id": "T-146",
  "mode": "constructor",
  "combination_mode": "simple",

Где:

"id" - идентификатор теста

"mode" - режим создания объектов датасета:
	- если "mode" = "constructor", объект создаётся конструктором
	- если "mode" = "fields", объект создаётся пустой, далее задаются поля и свойства
	
"combination_mode" - режим комбинирования правил создания датасета:
	- если "combination_mode" = "simple", размер датасета равен max(число_значений_правила[i])
	- если "combination_mode" =  "all-to-all", датасет будет состоять 
	из всех возможных комбинаций по заданным правилам
	
Так если у нас 3 правила, которые предполагают 2, 4, 3 возможных значений поля (аргумента), тогда в режиме: 
	при "combination_mode" = "simple" будет 4 объекта, возможные варианты значений 1 и 3 правил обычно зацикливаются
	при "combination_mode" =  "all-to-all" будет все 24 объекта
	
	
# 1.1. Правила генерации

Правила описываются в массиве далее:

  "combination_mode": "simple",
  "rules": [
    {
      "field": "FieldInt",
      "values": [ 1, 2, 3, 4, 5 ]
    },
    {
      "field": "FieldFloat",
      "field_type": "System.Single",
      "value": -23.2
    },
    {
      "field": "FieldDouble",
      "range": [ -1, 1 ],
      "step": 0.5
    },
    {
      "field": "FieldString",
      "function": "created_function"
    }
  ]
  
При "mode" = "fields", имя поля обязательно указывается в "field"

При "mode" = "constructor" имя поля не обязательно, порядок правил - порядок аргументов конструктора

Содержимое правил имеет следующие компоненты:

"field_type" - тип поля или аргумента конструктора, необязателен в "mode" = "fields"
"value" - значение, одинаковое для всех объектов
"values" - массив возможных значений
"range" + "step" - область возможных значений
"function" - возвращаемое значение функции, принимающей int, подробнее см. ниже

"field_type" следует обязательно указывать при использовании конструктора, так конфликтовать могут типы int/long, float/double
При задании через поля и свойства тип может определяться автоматически, но конструктору требуется строгое соответствие типов.

# 1.2. Функции

"function" в правилах - это имя используемой функции из словаря GenerateFunctions.
Считается, что у функции всего 1 вариация, если не указано иного.
Функция не зацикливается на себя, в отличие от других правил.

Чтобы использовать функцию для генерации датасета:

[GenerateFunction("NameInJSON")]
static object NameInCode(int i) { ... }
// Опишите статическую функцию int => object и добавьте атрибут GenerateFunction

Или пропишите в вашем тестовом методе до выполнения теста:
GenerateFunctions.AddFunc("created_function", i => $"Name {i}");

Не забывайте, тип возвращаемого object особенно важен, особенно для конструктора.

Правило, если используется функция, имеет следующий формат:
	{
      "field": "FieldString",
      "function": "NameInJSON"
    }
Опишет задание полю FieldString объекта значения created_function(i), где i от 0 до n - 1 (размер датасета)

i - индивидуален для каждого объекта датасета!

Так если в датасете 10 объектов, для каждого будет вызван свой аргумент.

Так как число комбинаций от функции по умолчанию 1, чтобы
исправить эту проблему, необходимо указать function_calls
	{
      "field": "FieldString",
      "function": "created_function",
	  "function_calls": 6
    }
Допустим, это единственное правило, без "function_calls", будет всего 1 объект: f(0),
но если указать "function_calls": 6, тогда это будут f(0), f(1), f(2), f(3), f(4), f(5).

Если, будет 2-е правило с n комбинациями, 
тогда аргумент будет:
- при "all-to-all" 	0 до 6n
- при "simple" 		max(6, n)

Функции - единственный способ передавать объекты, которые нельзя описать в JSON
(То есть типы, отличные от примитивов).


# 1.3. Правила проверок

После массива "rules" идут масивы "assert_before_lambda" и "assert_after_lambda", проверки
до и после пользовательской логики.

Если проверка провалена, будет исключение с соответствующим сообщением.


Пример проверки:
  {
	"function": "GetSecret",
	"type_assert": "equals",
	"result": null
  }
Вызовет функцию GetSecret без аргументов и проверит, чтобы вернула нуль.


"type_assert" может принимать следующие значения:

- "equals": сравнит значения (ожидаемое - реальное), должны быть равны
- "unequals": должны быть не равны
- "more": реальное строго больше ожидаемого
- "lesser": реальное строго меньше ожидаемого
- "regex": соответствие строке из регулярного выражения C# (регулярное выражения скопировать в параметр "value")
- "function": соответствие значению возврата функции(int i), i - порядковый номер в датасете. 
	См. атрибут [GenerateFunction("имя")] или 
	GenerateFunctions.AddFunc(string key, Func<int, object> func)

"type_assert" указывать НЕ обязательно, по умолчанию значение "equals".


Параметры проверок, отвечающие за получаемое значение:

"function" - вызов функции, если "args" одномерный массив, тогда для каждого объекта единыц набор аргументов, 
			если двумерный - аргументы для каждого объекта свои и соответствуют номеру в датасете

		К примеру, "args": [
		  [ -1 ],
		  [ -2 ],
		  [ 5 ],
		  [ 4 ],
		  [ 5 ]
		]  - у нас в датасете должно быть 5 объектов, каждому вызовем функцию с аргументом соответственно списку

"method" - вызов метода с аргументами "args", проверка отсутствует
"field" - проверка значения поля объекта

Если аргументы отсутствуют, тогда объявление "args": [] не обязательно.

Параметры проверок, отвечающие за ожидаемое значение:

"result" - для всех результатов вызовов функции одинаковая проверка
"results" - для каждого вызова, соответственно номеру в датасете, своё ожидаемое значение

"value" - ожидаемое значение поля у каждого объекта одинаковое
"values" - ожидаемое значение поля каждого объекта индивидуально

"exception" - имя ожидаемого исключения при вызове функции или метода
"exceptions" - имя ожидаемого исключения (соответственно номеру в датасете и в списке)
 Если параметр отсутствует или значение null - исключение не ожидается

# 1.4. Пример тестирования функции CheckEmail

Допустим, нужно протестировать метод для проверки почты: 

    internal class EmailVerificator
    {
        private string email;
        private static Regex reg = new Regex(@"^[a-zA-Z0-9]+([-._][a-zA-Z0-9]+)*@[a-zA-Z0-9]+([-.][a-zA-Z0-9]+)*\.[a-zA-Z]{2,}$");
        public EmailVerificator(string Email)
        {
            email = Email;
        }

        public bool Check() // ВОТ, ЧТО НУЖНО ПРОТЕСТИРОВАТЬ
        {
            return email != null && reg.IsMatch(email);
        }
    }
	
	
Создаём код теста. Дополнительная логика не требуется, потому лямбда будет пустой:

		[TestMethod] //Атрибут из Microsoft.VisualStudio.TestTools.UnitTesting
        public void EmailCheck()
        {
            var content = File.ReadAllText(
                "C:\\Users\\Admin\\source\\repos\\JsonUnitSimplifier__UNIT_TESTS_FOR_LIBRARY\\JSON\\" +
                "EmailCheckPositive.json"); // Укажите свой путь
            TestByJSON.TestObject<EmailVerificator>(content, o => {});

            content = File.ReadAllText(
                "C:\\Users\\Admin\\source\\repos\\JsonUnitSimplifier__UNIT_TESTS_FOR_LIBRARY\\JSON\\" +
                "EmailCheckNegative.json"); // Укажите свой путь
            TestByJSON.TestObject<EmailVerificator>(content, o => {});
        }
				
				
Далее создаём соответствующие JSON для описания тестов, начнём с позитивного теста:

{
  "id": "Email Checker",
  "mode": "constructor",
  "combination_mode": "simple",
  "rules": [
    {
      "values": [ "daddy@gmail.com", "iamanidiot666@gnail.ru ...]
СТОП, ТАК ДЕЛАТЬ НЕЛЬЗЯ, нужно подготовить достаточно тестовых данных. Потому создадим тестовый набор функционально:

		[GenerateFunction("emails")] // Объявите где угодно, но добавьте атрибут
        static object Email(int i)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            const string domains = "gmail.com,yahoo.com,hotmail.com,example.com";
            int length = random.Next(20, 40);
            StringBuilder username = new StringBuilder(length);
            for (int j = 0; j < length; j++)
            {
                username.Append(chars[random.Next(chars.Length)]);
            }
            string[] domainList = domains.Split(',');
            string domain = domainList[random.Next(domainList.Length)];
            return $"{username.ToString()}{i}@{domain}";
        }
		
GenerateFunction - атрибут, который даёт библиотеке понять, что эта функция будет использована в тестах.
Обязательно, чтобы функция имела формат static object(int)
		
Указываем эту функцию в правилах (JSON):
{
  "id": "Email Checker",
  "mode": "constructor",
  "combination_mode": "simple",
  "rules": [
    {
      "function": "emails",
      "function_calls": 2000
    }
  ],
  "assert_after_lambda": [
    {
      "function": "Check",
      "args": [],
      "type_assert": "equals",
      "result": true
    }
  ]
}

# Расшифровка файла: 

1. "constructor" объекты буду созданы при помощи конструткора, в д.с. T(string)
2. "combination_mode" роли не играет, т.к. всего одно правило
3. Правило будет применено 2000 раз: первый аргумент конструктора - результат вызова emails(i) 
4. У каждого объекта вызовется Check(), проверка, везде должен быть результат true


Далее аналогично создадим негативный тест. Функцию неправильных адресов пропишите сами.

{
  "id": "Email Checker",
  "mode": "constructor",
  "combination_mode": "simple",
  "rules": [
    {
      "function": "emails_wrong",
      "function_calls": 6
    }
  ],
  "assert_after_lambda": [ (оба варианта идентичны, показаны для примера)
    {
      "function": "Check",
      "args": [],
      "type_assert": "unequals",
      "result": true
    }, 
    {
      "function": "Check", 
      "args": [],
      "type_assert": "equals",
      "result": false
    }
  ]
}

# Отлавливаем ошибку после запуска теста:

Если мы что-то перепутаем с названиями функций в атрибутах, будет ошибка реализации, получим исключение наподобие:
System.Exception: Assert of equals Check dataset[0] failed: Expected value 'True', but got 'False'.

Это значит, что на первой же проверке ошибка: то ли адрес невалидный, то ли функция проверки глючит


Может так получиться, что вылезет ошибка: System.Exception: Assert of equals Check dataset[342] failed: Expected value 'True', but got 'False'.

То есть предыдущие 342 были верные. Поменяем тестируемый класс и добавим ещё логику:
TestByJSON.TestObject<EmailVerificator>(content, o => {
                
                Console.WriteLine(o.email); // Пользовательская логика           
            });

Важно в JSON сделать проверки в "assert_after_lambda", а не в "assert_before_lambda", иначе иcключение 
будет выброшено раньше пользовательской логики с выводом в консоль.

После запуска теста при ошибке перед исключением мы в консоли (стандартный вывод в тестах) увидим
тот адрес, на котором произошла ошибка.

Если увидим что-то вроде: 9VIHYQrIr489@hotmail..com, то неверно cгенерирован датасет
Если увидим что-то вроде: 9VIHYQrIr489@hotmail.com, то неверно работает тестируемая функция


# 1.5. Примечания, возможные ошибки

- Если проверяем иные условия, "function_calls" указывать не нужно, всё равно имейл для всех останется уникальным
- Не путайте, проверка поля - value (values), проверка результата - result (results) 
- При проверке текстовых значений желательно использовать тип сравнения "type_assert": "regex", "value/result": "^[a-zA-Z0-9]..."
- Если происходит ошибка до создания датасета, значит, ошибка в порядке аргументов, в их типах, или поле по имени не найдено
- Для правильной работы "fields" поля объявлять как "public int hehe {get; set;}", а не как "public int hehe;"
- В правилах генерации не устанавливайте "value": null, это приведёт к ошибке, правильнее "values": [null]


# 2. Тестирование слоистой архитектуры

Данный раздел рекомендуется пропустить, если вы собираетесь тестировать только 1 класс.
Если нужен пример работы "all-to-all", перейдите в конец главы.


# 2.1. Определение

Слоистая архитектура включает в себя множество различных паттернов, включая: MVVM, MVC, MVP, Repository, Service Layer

В данном контексте у слоистой архитектуры тестируется конкретный слой Service, который управляет набором объектов класса Class.

На практике это может быть класс Service, управляющий репозиторием объектов Class из модели базы данных. 
Формат класса службы выглядит следующим образом:

internal class TestedService
{
    ITestedRepository testedRepository;

    public TestedService(ITestedRepository testedRepository)
    {
        this.testedRepository = testedRepository;
    }
	
	public void Add(TestedObject testedObject)
    {
        testedRepository.Save(testedObject);
    }
	
	...
}

# 2.2. Цели проверок

Тестируемая служба может содержать различную логику, проверки, вычисления, работу с объектами. 

Обычно создаётся затычка, реализующая интерфейс ITestedRepository, далее идёт заполнение тестовыми данными,
после чего идут непосредственно сами тесты, проверки, вызовы функций и многое другое.

Библиотека JsonUnitSimplifier предлагает упростить этот процесс.


# 2.3. #Пример тестирования слоистой архитектуры TestedService-TestedObject

string content = File.ReadAllText(
    "C:\\Users\\Admin\\source\\repos\\JsonUnitSimplifier__UNIT_TESTS_FOR_LIBRARY\\JSON\\" +
    "Layered.json");

TestByJSON.TestLayeredService<TestedObject, TestedService>(content, new TestedService(new TestedRepositoryMock()),
    a, b) => { a.Add(b); }, (a, b) => { /* Доп. Логика, вызывается всего 1 раз */});
	
TestedObject - обычно класс модели БД
TestedService - тестируемая служба


Обратите внимание, ко всем проверкам обязательно дополнение "target":
	"objects" - проверка объектов датасета
	"service" - проверка объекта службы
	"service-to-object" - вызов функции или метода службы, в начало массива args добавляется ссылка на объект датасета 
							(вызовется для каждого объекта датасета соответственно)
							
Пример описания теста (рекомендуется скопировать текст в отдельное окно, чтобы было проще сравнивать с расшифровкой):

{
  "id": "No id, I am lazy",
  "mode": "fields",
  "combination_mode": "all-to-all",
  "rules": [
    {
      "field": "FieldInt",
	  "field_type": "System.Int32",
      "values": [ 1, 2, 3, 4, 5 ]
    },
    {
      "field": "FieldFloat",
	    "field_type": "System.Single",
      "value": -23.2
    },
    {
      "field": "FieldDouble",
      "range": [ 11, 15 ],
      "step": 1
    },
    {
      "field": "FieldString",
      "value": "created"
    }
  ],
  "assert_before_lambda": [
  {
	"function": "GetSecret",
	"target": "objects",
	"type_assert": "equals",
	"args": [],
	"result": null
  },
  {
	"method": "SetSecret",
	"target": "objects",
	"args": ["hi-hi"]	
  },
  {
	"function": "GetSecret2",
	"args": [2],
	"type_assert": "regex",
	"target": "objects",
	"result": ".hi-hi!!!."	
  }
  ],
  "assert_after_lambda": [
    {
      "function": "Len",
      "args": [],
      "type_assert": "equals",
      "target": "service",
      "result": 25
    },
    {
      "field": "FieldInt",
      "args": [],
      "type_assert": "equals",
      "target": "objects",
      "values": [ 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4, 5, 5, 5, 5, 5 ]
    },
    {
      "function": "GetDouble",
      "args": [],
      "type_assert": "equals",
      "target": "service-to-object",
      "results": [ 11, 12, 13, 14, 15, 11, 12, 13, 14, 15, 11, 12, 13, 14, 15, 11, 12, 13, 14, 15, 11, 12, 13, 14, 15 ]
    }
  ]
}

# Расшифровка:

"mode": "fields" - Объекты модели создаются пустые, потом заполняются поля 
Будут созданы все комбинации по правилам "combination_mode": "all-to-all", в данном случае, 5 х 1 х 5 х 1 = 25

Полю "field": "FieldInt" будет задаваться значение из списка [ 1, 2, 3, 4, 5 ] по внешнему циклу перебора
"FieldFloat" будет задаваться -23.2, строгое кастование к типу System.Single
"FieldDouble" будет от 11 до 15 с шагом 1 (всего 5 комбинаций)
"FieldString" для всех объектов равно "created"

До дополнительной логики:

У всех объектов объектов модели ("target": "objects") вызвать:
GetSecret() - должет быть возврат null 
SetSecret("hi-hi")
GetSecret() - должет быть возврат, соответственно регулярному выражению ".hi-hi!!!."

После дополнительной логики:

У службы ("target": "service") будет вызвана функция "Len", должно вернуть 25
У всех объектов будет проверено значение поля "field": "FieldInt" (обратите внимание, значения идут по внешнему циклу)

И в конце: "target": "service-to-object"
Для всех объектов будет вызов функции службы GetDouble(объект), обратите внимание, ожидаемые значения идут по внутреннему циклу

# Примечания:

- Хотя показанный пример логики смысла не имеет, у класса службы может быть важная логика:
	- Обновления данных
	- Выполнения рассчётов на основе предоставленного объекта
	- Проверки данных
	- Редактирования передаваемого объекта
- Самый внешний цикл - самое первое правило, внутренний - последнее
- НЕ РЕКОМЕНДУЕТСЯ делать проверку через "results" и "values", используйте условные операторы и разделение на разные тесты


# 3. Генерация датасета функционально

Если вы не хотите создавать JSON файлы и хотите обойтись только кодом, используйсте класс Constructor.

Указывается размер датасета и правила создания объектов.

// Датасет создаётся функционально
var dataset_array = 
	Constructor.CreateByTemplate<TestedObject>(10, x => new TestedObject { FieldInt = x * 3 });

// Вызов конструктора по аргументам конструктора
var dataset_array2 =
	Constructor.CreateByArgs<TestedObject>(10, new Func<int, object>[] {
		i => i + 5,
		i => i * i,
		i => $"name {i}"
	});
	
Отдельное внимание следует обратить на возвращаемые типы, они всегда должны соответствовать конструктору.


# 4. Полностью автоматизированное тестирование (no code)

Возможно проводить тесты, вызывая особые методы для автоматического проведения тестов.
Для этого требуется прочитать все файлы и передать содержимое TestByJSON.AutoTestByJSON
(возможна передача массива json строк, а возможно и содержимое конкретного файла):

[TestMethod]
public void AutoTest()
{
    string content = File.ReadAllText(
        "C:\\Users\\Admin\\source\\repos\\JsonUnitSimplifier__UNIT_TESTS_FOR_LIBRARY\\JSON\\" +
        "Auto.json");
        TestByJSON.AutoTestByJSON(content);
    }
}

Обязательно в начале JSON файла обязательно следует указать имена классов и вашей сборки:
  
  "id": "No id, I am lazy",
  "mode": "fields",
  "combination_mode": "simple",
  "classes": [
    "JsonUnitSimplifier__UNIT_TESTS_FOR_LIBRARY.TestedObject,JsonUnitSimplifier__UNIT_TESTS_FOR_LIBRARY",
    "JsonUnitSimplifier__UNIT_TESTS_FOR_LIBRARY.TestedService,JsonUnitSimplifier__UNIT_TESTS_FOR_LIBRARY",
    "JsonUnitSimplifier__UNIT_TESTS_FOR_LIBRARY.TestedRepositoryMock,JsonUnitSimplifier__UNIT_TESTS_FOR_LIBRARY",
    "Add"
  ],
  
В случае, если вы тестируете один класс, как в первом примере, "classes" будет состоять из 1 элемента - укащателя на ваш класс.

Если это тестирование слоистой архитектуры, тогда порядок следующий (как в примере выше):

1. Имя объекта модели, из которых составляется датасет
2. Имя класса тестируемой службы
3. Имя класса затычки с пустым конструтктором, передаваемая в единственный аргумент конструктора класса службы
4. Имя метода добавления в службу объектов датасета, тип: void(TestedObjectClass1)
