# OneScript-AspNetEngine

Настоящий проект представляет собой набор библиотек, реализующий расширенный функционал http-сервисов OneScript, который можно использовать совместно со штатными возможностями.
Библиотеки является полностью совместимыми с "родными" http-сервисами OneScript и могут быть установлены поверх штатных библиотек.

## ASPNETHandler
Реализация расширенного функционала в обработчике http-сервисов ASPNETHandler. 

[Подробнее...](https://github.com/jdeshin/OneScript-AspNetEngine/blob/master/ASPNETHandler/README.MD).

## WebBackgroundJobs
Библиотека, реализующая механизм фоновых заданий в http-сервисах OneScript, аналогично платформе 1С:Предприятие.

[Подробнее...](https://github.com/jdeshin/OneScript-AspNetEngine/blob/master/WebBackgroundJobs/README.MD)

## DataProcessors
Библиотека, реализующая механизм обработок в http-сервисах OneScript, аналогично платформе 1С:Предприятие.

[Подробнее...](https://github.com/jdeshin/OneScript-AspNetEngine/blob/master/DataProcessors/README.MD)

## Enums
Библиотека, реализующая механизм перечислений в http-сервисах OneScript, аналогично платформе 1С:Предприятие.

[Подробнее...](https://github.com/jdeshin/OneScript-AspNetEngine/blob/master/Enums/README.MD)

## Templates
Библиотека, реализующая механизм макетов в http-сервисах OneScript, аналогично платформе 1С:Предприятие. 

[Подробнее...](https://github.com/jdeshin/OneScript-AspNetEngine/blob/master/Templates/README.MD).

## SqlDataProcessor
Библиотека, реализующая переносимый на уровне исходного кода механизм работы с СУБД в http-сервисах OneScript.

Библиотека основана на библиотеке [oscript-sql](https://github.com/jdeshin/OneScript-AspNetEngine/blob/master/DataProcessors/README.MD)  

[Подробнее...](https://github.com/jdeshin/OneScript-AspNetEngine/blob/master/SqlDataProcessor/README.MD)

## HttpMeans
Библиотека, реализующая дополнительные методы и свойства для работы с http и web.

[Подробнее...](https://github.com/jdeshin/OneScript-AspNetEngine/blob/master/HttpMeans/README.MD)

## AspNetEngine
Служебная библиотека, реализующая пул объектов HostedScriptEngine в http-сервисах для выполнения фоновых заданий в отдельных потоках.
Данная библиотека используется библиотеками ASPNetHandler и WebBackgroundJobs

[Подробнее...](https://github.com/jdeshin/OneScript-AspNetEngine/blob/master/AspNetEngine/README.MD)