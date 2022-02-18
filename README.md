# StrippingByVariantCollection

プロジェクト内にあるShaderVariantCollectionを探してきて、登録されていないVariantはすべてビルドから除外します。<br />
ShaderVariantCollectionにシェーダーを登録していなかった場合は何もしません。

メニューの「UTJ/ShaderVariantStrip」の設定ウィンドウを変更することが出来ます。<br />

![alt text](Documentation~/ConfigWindow.png)


ビルドした時に、どのバリアントを入れて、どのバリアントを除外したか？をログとして残します。<br />
プロジェクト直下の「ShaderVariants/Builds/タイムスタンプ」ディレクトリ以下に書き出します。<br />

連続でビルドをする場合などは…。<br />
タイムスタンプとかの部分をリセットするために「UTJ/ShaderVariantStrip/ResetInfo」を呼び出してください。<br />

<br />

参考：
こちらはスクリプタブルシェーダーバリアントの除去を使っています<br />
https://blogs.unity3d.com/jp/2018/05/14/stripping-scriptable-shader-variants/
