# HttpMeans

## Описание

Библиотека реализует расширенные функции для работы с web и http. В качестве основы используется пространство имен [System.Web](https://msdn.microsoft.com/en-us/library/gg145018%28v=vs.110%29.aspx?f=255&MSPPError=-2147217396)
Для доступа к свойствам и методам используется тип СредстваHTTP.

## Установка

Установка осуществляется копированием файлов dll в папку Bin веб-приложения.
Затем, необходимо подключить библиотеку, добавив нижеследующую строку в секцию ```<appSettings>```, файла web.config:

```bsl
<add key="HttpMeans" value="attachAssembly" />
```

## Функционал

### СредстваHTTP

Основной тип для работы с расширенным функционалом.

Методы:

Свойства:

-ТекущийКонтекст. Возвращает контекст текущего запроса (HTTPСервисКонтекст). Основа - свойство [System.Web.HttpContext.Current](https://msdn.microsoft.com/ru-ru/library/system.web.httpcontext.current%28v=vs.110%29.aspx?f=255&MSPPError=-2147217396).

Методы:

-ПолучитьФизическийПутьИзВиртуального. Преобразует виртуальный путь ф физический. Основа - метод [HttpContext.Current.Server.MapPath](https://msdn.microsoft.com/ru-ru/library/system.web.httpserverutility.mappath(v=vs.110).aspx)

### HTTPСервисКонтекст

Реализует контекст текущего web-запроса.

Методы:

Свойства:

- ФизическийПуть. Содержит физический путь, соответствующий запрашиваемому url. Основа - свойство [HttpContext.Current.Request.PhysicalPath](https://msdn.microsoft.com/ru-ru/library/system.web.httprequest.physicalpath(v=vs.110).aspx)
- АдресКлиента. Содержит ip-адрес клиента, создавшего запрос. Основа - свойство [HttpContext.Current.Request.UserHostAddress](https://msdn.microsoft.com/ru-ru/library/system.web.httprequest.userhostaddress(v=vs.110).aspx)
- Сессия. Содержит параметры текущей сессии (HTTPСервисПараметрыСессии). Основа - свойство [HttpContext.Current.Session](https://msdn.microsoft.com/ru-ru/library/system.web.httpcontext.session(v=vs.110).aspx)

### HTTPСервисПараметрыСессии

Методы:

Свойства:

-Количество. Получает число элементов в коллекции состояний сеанса. Основа - свойство [HttpContext.Current.Session.Count](https://msdn.microsoft.com/ru-ru/library/system.web.sessionstate.httpsessionstate.count(v=vs.110).aspx)

## Использование

```bsl
// Данный функционал доступен только в OneScript
//<OneScript>
СредстваВеб = Новый СредстваHTTP;

IPАдрес = СредстваВеб.ТекущийКонтекст.АдресКлиента;
ФизическийПуть = СредстваВеб.ТекущийКонтекст.ФизическийПуть;
ФизическийПуть = СредстваВеб.ПолучитьФизическийПутьИзВиртуального("~/");
//<!OneScript>
```