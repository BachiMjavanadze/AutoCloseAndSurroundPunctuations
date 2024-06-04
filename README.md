# Auto Close & Surround Punctuations

This is `Visual Studio IDE` extension. It brings `VSCode`'s Punctuations "Auto Surround" feature and fixes "Auto Close" bug.

Here is "Auto Surround" feature:

![1](https://github.com/BachiMjavanadze/AutoCloseAndSurroundPunctuations/assets/38082501/1fb7746d-38db-40dd-8c24-a75e63ae1596)

You could argue that `Visual Studio` already has this functionality built in, but it's not quite suitable because it can't do this:

![2](https://github.com/BachiMjavanadze/AutoCloseAndSurroundPunctuations/assets/38082501/395c8103-9e90-4dc4-8c4a-f0bb83502e8a)

Also this extension fixes "Auto Close" bug in Visual Studio; Here is the bug:

![3](https://github.com/BachiMjavanadze/AutoCloseAndSurroundPunctuations/assets/38082501/c23ecbee-2d57-4942-b062-6ac5343db0e9)

and this is correct behavier:

![4](https://github.com/BachiMjavanadze/AutoCloseAndSurroundPunctuations/assets/38082501/c120f1e9-d993-4989-8555-521ee0e820e4)

To avoid overlapping I recomend disable `Visual Studio`'s standard behaviors:

`Automatic brace compilation`
and
`Automatically surround selection when typing quotes or brackets`

P.S. This extension is a clone of [AutoSurround](https://marketplace.visualstudio.com/items?itemName=reduckted.AutoSurround), but the author did not receive my pull request, so I decided to add [functionality](https://github.com/reduckted/AutoSurround/issues/2) and create a new extension.
