### Принцип работы утилиты

- UI утилиты отделён от логики.
- По нажатию на кнопку «Scan Project Assets», создаётся экземпляр класса `FindMissingReferenceTool` и вызывается метод ScanProject, в который по ссылке передаётся параметр `List<BrokenPrefab>`.
- С помощью метода `AssetDatabase.FindAssets("t:Prefab")`  осуществляется поиск всех префабов в проекте
- Каждый элемент полученного списка префабов проверяется на наличие missing префабов, скриптов и ссылок в скриптах
- Также рекурсивно обходятся все дочерние объекты в префабах

### Поиск missing ссылок
- Для поиска missing-ссылок в объекте, у GameObject вызывается метод `GetComponents<Component>()`, возвращающий массив всех компонентов, прикреплёных к GameObject
- В качестве универсального параметра используется `Component`, потому что данный класс является базовым для любого скрипта, прикреплёного к GameObject
- С помощью предоставляемых движком Unity классов SerializedObject и SerializedProperty утилита получает перечислитель, предоставляющий доступ ко всем сериализованным полям данного компонента
- Каждое сериализованное поле (свойство компонента) проверяется на missing ссылку

### Способы реализации
- Поиск префабов можно осуществлять с помощью AssetDatabase, либо QuickSearch. Выбран AssetDatabase, так как в Unity 2020 QuickSearch требует писать собственные SearchContext и SearchProvider, это оверинжиниринг для данной задачи. Кроме того, AssetDatabase производительней, чем QuickSearch (как я понял, потому что в основе QuickSearch лежит получение всех ассетов с помощью AssetDatabase, поверх чего уже применяются поисковые выражения SearchProvider'а)
- Поиск missing ссылок осуществляется с помощью SerializedObject/SerializedProperty, которые созданы для работы с компонентами в «общем виде», без привязки к конкретной реализации. Других способов я не нашёл, в голову приходит только идея вручную писать хэндлеры для проверки полей каждого нужного нам класса.

### Скриншоты
![Стартовое окно](https://github.com/kolosovti/find-missing-reference/raw/master/Documentation/Images/startup-window.png)
![Результат выполнения](https://github.com/kolosovti/find-missing-reference/raw/master/Documentation/Images/result-window.png)