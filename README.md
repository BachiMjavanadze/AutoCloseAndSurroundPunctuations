# Auto Close & Surround Punctuations

The extension brings `VSCode`'s Punctuations "Auto Surround" feature to Visual Studio:

source\AutoSurround\Resources\1.gif

You could argue that `Visual Studio` already has this functionality built in, but it's not quite suitable because it can't do this:

source\AutoSurround\Resources\2.gif

Also this extension fixes "Auto Close" bug in Visual Studio; Here is the bug:

source\AutoSurround\Resources\3.gif

and this is correct behavier:

source\AutoSurround\Resources\4.gif

To avoid overlapping I recomend disable `Visual Studio`'s' standard behaviors:

`Automatic brace compilation`
and
`Automatically surround selection when typing quotes or brackets`

P.S. This extension is a clone of `AutoSurround` (https://marketplace.visualstudio.com/items?itemName=reduckted.AutoSurround), but the author did not receive my pull request, so I decided to add functionality (https://github.com/reduckted/AutoSurround/issues/2) and create a new extension.
