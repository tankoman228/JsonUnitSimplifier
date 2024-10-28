Чтобы было проще разобраться, составлен тестовый проект ServersAndHosts, на котором использована библиотека. Итак, введение в туториал (гайд, инструкцию).

Ссылка на проект: https://github.com/tankoman228/JsonUnitSimplifier_Sample_ServersAndHosts
В корневой папке будет лежать скрипт БД (database_script_ms_sql.sql)

В директории Tests находятся сами юнит-тесты и соответствующие JSON-файлы. Архитектура проекта слоистая, разделена на: UI <-> службы (логика) <-> репозитории (работа с БД)

Преимущества библиотеки JsonUnitSimplifier:

1. Код становится короче, чем больше тест, тем заметнее разница
2. Простейшие проверки выносятся в JSON
3. Тест легко модифицировать, дополнять, изменять датасет и правила генерации.

Содержание:

1. Other.AsyncManager.TryAsyncOrReturnError(Action action)

2. RepositoryMock
2.1 GetById(int id)
2.2 Add(T entity)
2.3 Delete(int id)

3. ComponentService 
3.1 GetComponents()
3.2 SearchComponent(string search)
3.3 RemoveComponent(string component)


4. ComponentTypeService
4.1 GetComponentTypes()
4.2 IdOrAddComponentTypeIfNotExists(string name)

1. Other.AsyncManager.TryAsyncOrReturnError(Action action)

Протестируем отдельный класс. Откройте проект ServersAndHosts, файл Other\AsyncManager.cs


У класса есть конструктор AsyncManager(string where, Action<string> error), 
по этому конструктору будет создан датасет.

Начнём с создания файла AsyncManager.json. Для проведения теста создадим 3*2=6 объектов:
{
  "id": "Async Manager",
  "mode": "constructor",
  "combination_mode": "all-to-all",
  "rules": [
    {
      "values": ["place A", "place B", "place Ы"]
    },
Но описать Action<string> примитивами не выйдет. Потому потом определим функцию, которая вернёт объект этого типа. Для каждого "where" пусть будет по 2 вызова функции.
    {
      "function": "action_error",
      "function_calls": 2
    }
  ],

Для справки, датасет по этим правилам:

new AsyncManager("place A", action_error(0));
new AsyncManager("place A", action_error(1));
new AsyncManager("place B", action_error(2));
new AsyncManager("place B", action_error(3));
new AsyncManager("place Ы", action_error(4));
new AsyncManager("place Ы", action_error(5));

Перейдём к описанию тестов. 

  "asser_before_lambda": [
    {
      "field": "Where",
      "type_assert": "regex",
      "value": "place (A|B|Ы)"
    }
  ]
Так проверим, что "Where" содержит верное значение.

Перейдём к коду (также см. файл Unit_AsyncManager.cs в папке Tests)
    
    [TestClass]
    public class Unit_AsyncManager
    {

	// Путь к файлам тестов
        private const string PATH = "C:\\Users\\Admin\\source\\repos\\ServersAndHosts\\Tests\\TestForServersAndHostWithJSONUnitSimplifier\\JSON\\";

        [TestMethod] 
        public void WithLib()
        {
            string errors = ""; // Для проверки на ошибки

            // Для аргумента типа Action<string>
            GenerateFunctions.AddFunc("action_error", i => new Action<string>(x => errors += x));

            // Создание датасета и выполнение тестов
            TestByJSON.TestObject<AsyncManager>(
               File.ReadAllText(PATH + "AsyncManager.json"), o =>
               { // Своя логика для проверки параллельности
                   o.TryAsyncOrReturnError(() => {                   
                    Thread.Sleep(100); throw new Exception(o.Where);
                });
            });
            Assert.AreEqual(errors, ""); // TryAsync выполнится асинхронно, значит, сейчас пусто
            Thread.Sleep(230); // TryAsync выполняется
            Assert.IsTrue(errors.Length > 30); // Ошибки есть? А если не найду?
        }

Полный JSON файл:
{
  "id": "Async Manager",
  "mode": "constructor",
  "combination_mode": "all-to-all",
  "rules": [
    {
      "values": [ "place A", "place B", "place Ы" ]
    },
    {
      "function": "action_error",
      "function_calls": 3
    }
  ],
  "asser_before_lambda": [
    {
      "field": "Where",
      "type_assert": "regex",
      "value": "place (A|B|Ы)"
    }
  ]
}


В данном тесте потребовалась пользовательская логика только для проверки асинхронности.
Пример теста без использования библотеки - в конце Unit_AsyncManager.cs.

Конечно, на этом примере преимущества заметны очень слабо, но чем у класса больше проверяемых полей, функций и методов и чем проще эти проверки, тем сильнее удастся сократить тест. Да и формат JSON воспринимается проще программного кода.


2. RepositoryMock<T>

Тестируем класс затычки БД. Включает в себя больше 1 слоя (управляет другими объектами), потому используем TestByJSON.TestLayeredService. Service - в одном экземпляре, это будет сам класс. А тестировать будем на классе Entity.server

TestByJSON.TestLayeredService<server, RepositoryMock <server>> (
    PATH + "ReposMock.json",
    new RepositoryMock<server>(),
    (a, b) => a.Add(b),
    (repos, dataset) => {});

Будет создано множество server, каждый будет передан в затычку через Add. 

Пользовательская дополнительная логика в этом тесте не понадобится, потому сразу к ReposMock.json

"mode": "fields" - объекты модели создают только через поля
"combination_mode": "simple" - нам не нужно проверять все комбинации

{
  "id": "Repository Mock",
  "mode": "fields",
  "combination_mode": "simple",
  "classes": [ "Namespace.NoNeedInThisSituation" ],
  "rules": [
    {
      "field": "address",
      "values": [ "192.168.3.73", "192.168.3.74", "192.168.3.80" ]
    },
    {
      "field": "name_in_network",
      "values": [ "comp", "servak", "nout" ]
    },
    {
      "field": "ram_total_mb",
      "range": [ 1024, 3072 ],
      "step":  1024
    },
    {
      "field": "cpu_frequency_mhz",
      "value": 4096
    }
  ],
"assert_before_lambda": [ ...
Получен простенький датасет из 3-х элементов, этого хватит, чтоб проверить затычку.

Далее вызываем на затычке методы интерфейса IRepository
T GetById(int id);
int Add(T entity);
void Delete(int id);

   {
      "target": "service",
      "function": "GetById",
      "args": [2],
      "type_assert": "unequals",
      "result": null
    },
GetById должен вернуть ссылку на существующий объект

    {
      "field": "id",
      "target": "objects",
      "results": [ 0, 1, 2 ]
    },
Проверим, задала ли затычка id объектам датасета

    {
      "function": "Add",
      "target": "service-to-object",
      "results": [ 3, 4, 5 ]
    }
Затычка должна вернуть псевдо-id для объектов при добавлении (датасет не изменяется после вызова)

    {
      "method": "Delete",
      "target": "service",
      "args": [ 1 ]
    },
    {
      "method": "GetById",
      "target": "service",
      "args": [ 5 ],
      "exception": "Exception"
    }
Затычка должна удалить объект, при попытке доступа - ошибка.

За кадром расширим датасет. Итого, тест выглядит так:
{
  "id": "Repository Mock",
  "mode": "fields",
  "combination_mode": "simple",
  "rules": [
    {
      "field": "address",
      "values": [ "192.168.3.73", "192.168.3.74", "192.168.3.80", "192.168.3.82" ]
    },
    {
      "field": "name_in_network",
      "values": [ "comp", "servak", "nout", "nout2" ]
    },
    {
      "field": "ram_total_mb",
      "range": [ 1024, 4096 ],
      "step": 1024
    },
    {
      "field": "cpu_frequency_mhz",
      "value": 4096
    }
  ],
  "assert_before_lambda": [
    {
      "target": "service",
      "function": "GetById",
      "args": [ 2 ],
      "type_assert": "unequals",
      "result": null
    },
    {
      "target": "objects",
      "field": "id",
      "values": [ 0, 1, 2, 3 ]
    },
    {
      "function": "Add",
      "target": "service-to-object",
      "results": [ 4, 5, 6, 7 ]
    },
    {
      "method": "Delete",
      "target": "service",
      "args": [ 5 ]
    },
    {
      "method": "GetById",
      "target": "service",
      "args": [ 5 ],
      "exception": "Exception"
    }
  ]
}
Таким образом будет протестирован класс RepositoryMock<server>. И если интерфейс класса расширится и добавятся (изменятся) функции, отредактировать тест будет куда проще, чем редактировать код.

В файле Unit_RepositoryMock.cs есть пример того же теста без использования библиотеки. 

---------------------------------------------------------
| SEO анализ	| символов всего | без пробелов | строк |
---------------------------------------------------------
| JSON + CS	| 1024		 | 886 		| 54+9	|
| Чистый CS	| 1133    	 | 1021		| 57	|
---------------------------------------------------------

Итого:
1. Программный код сокращается, даже если учесть JSON файл
2. У формата JSON читаемость и лучше, чем у кода, более чёткая структура
3. Если поменяется модель данных (класс server), редактирование теста займёт считанные секунды


ComponentService и ComponentTypeService будут проверены в полностью автоматическом режиме. Библиотека способна сама определить классы, если указать пути к классам.

Программный код тестового метода выглядит следующим образом
        [TestMethod]
        public void Automatic()
        {
            TestByJSON.AutoTestByJSONs(PATH);
        }

По всес JSON файлам в директории будут проведены тесты.

Поскольку уже имеющийся RepositoryMock generic, придётся объявить заглушки как обычные классы:

public class RepositoryMockComponent : RepositoryMock<component> { };
public class RepositoryMockComponentType : RepositoryMock<component_type> { };

3. ComponentService

Начало файла теста Auto\ComponentService.json:
{
  "id": "ComponentService",
  "mode": "fields",
  "combination_mode": "simple",
  "classes": [
    "ServersAndHosts.Entity.component,ServersAndHosts",
    "ServersAndHosts.Service.ComponentService,ServersAndHosts",
    "TestForServersAndHostWithJSONUnitSimplifier.RepositoryMockComponent,TestForServersAndHostWithJSONUnitSimplifier",
    "AddComponent"
  ],
  "rules": [
    {
      "field": "name",
      "values": [ "i4-1388", "kingston a-3982 677GB" ]
    }
  ],
  "assert_before_lamba": [

  ]
}

Порядок элементов "classes" важен.
Первый элемент - объект модели, через запятую имя сборки.
Второй - тестируемая служба (есть конструктор с аргументом интерфейсом репозитория)
Третий - класс-затычка, реализует интерфейс репозитория (должен быть пустой конструктор)
Четвёртый - имя функции добавления в датасет у ComponentService.

Далее этот файл можно копировать в той же директории и изменять массив "assert_before_lamba" (или "assert_after_lamba", пользовательской логики при атоматическом тестировании нет).

Далее показаны assert для каждого из тестируемых компонентов

3.1 GetComponents()

    {
      "function": "GetComponents",
      "target":  "service",
      "type_assert": "unequals",
      "result": null
    }
Просто проверим, возвращает ли хоть что-то

3.2 SearchComponent(string search)
    
    {
      "function": "SearchComponent",
      "target": "service",
      "args": [ "i4-1388" ],
      "type_assert": "unequals",
      "result": null
    },
    {
      "function": "SearchComponent",
      "target": "service",
      "args": [ null ],
      "exception": "ArgumentNullException"
    }
Чтобы находило, что есть, и чтобы выплёвывала null, 2 теста в одном

3.3 RemoveComponent(string component)

    {
      "function": "RemoveComponent",
      "target": "service",
      "args": [ "i4-1388" ]
    },
    {
      "function": "SearchComponent",
      "target": "service",
      "args": [ "i4-1388" ],
      "exception": "Exception"
    },
    {
      "method": "SearchComponent",
      "target": "service",
      "args": [ "kingston a-3982 677GB" ],
    }
Удаляем и смотрим, что компонент больше не найти.

Если при запуске возникли исключения, внимательно читайте их сообщения. Там указано, когда какой тест был провален и при каких условиях.

Напомню:
"target": "service" - тестирование службы
"target": "service-to-object" - вызов методов и функций службы, в начало массива аргументов добавляется объект класса модели из датасета
"target": "objects" - тестирование объектов датасета


4. ComponentTypeService

Начало:
{
  "id": "ComponentTypeService",
  "mode": "fields",
  "combination_mode": "simple",
  "classes": [
    "ServersAndHosts.Entity.component_type,ServersAndHosts",
    "ServersAndHosts.Service.ComponentTypeService,ServersAndHosts",
    "TestForServersAndHostWithJSONUnitSimplifier.RepositoryMockComponentType,TestForServersAndHostWithJSONUnitSimplifier",
    "AddComponentType"
  ]
}

Однако, класс не содержит метода, который можно было бы передать в качестве делегата. Потому его пришлось создать.
В данном случае есть 2 варианта - генерировать датасет не правилами, либо определить метод для добавления.

В класс службы добавляем метод
        private void AddComponentType(Entity.component_type componentType)
        {
            repository.Add(componentType);
        }

Вариант создания датасета, если использовать AddComponentType:
{
  "id": "ComponentTypeService with dataset",
  "mode": "fields",
  "combination_mode": "simple",
  "classes": [
    "ServersAndHosts.Entity.component_type,ServersAndHosts",
    "ServersAndHosts.Service.ComponentTypeService,ServersAndHosts",
    "TestForServersAndHostWithJSONUnitSimplifier.RepositoryMockComponentType,TestForServersAndHostWithJSONUnitSimplifier",
    "AddComponentType"
  ],
  "rules": [
    {
      "field": "typename",
      "values": ["RAM", "CPU", "SSD", "HDD"]
    }
  ]
}


Вариант 2 (rules должен быть пустым! "AddComponentType" должен быть объявлен, реализация не имеет значения. "mode": "constructor"):
{
  "id": "ComponentTypeService No dataset",
  "mode": "constructor",
  "combination_mode": "simple",
  "classes": [
    "ServersAndHosts.Entity.component_type,ServersAndHosts",
    "ServersAndHosts.Service.ComponentTypeService,ServersAndHosts",
    "TestForServersAndHostWithJSONUnitSimplifier.RepositoryMockComponentType,TestForServersAndHostWithJSONUnitSimplifier",
    "AddComponentType"
  ],
  "assert_before_lambda": [
    {
      "target": "service",
      "method": "IdOrAddComponentTypeIfNotExists",
      "args": [
        [ "RAM" ],
        [ "CPU" ],
        [ "SSD" ],
        [ "HDD" ]
      ]
    }
  ]
}

4.1 GetComponentTypes()
  
"assert_after_lambda": [
    {
      "target": "service",
      "function": "GetComponentTypes",
      "result": null,
      "type_assert": "unequals"
    }
  ]

4.2 IdOrAddComponentTypeIfNotExists(string name)




