lilycalInventory
====

# This project is under development and may be subject to breaking changes
# このプロジェクトは開発中であり破壊的な変更がされる場合があります

## 説明

このツールは非破壊でVRChatのアバターを編集するツールです。オブジェクトのオンオフ、BlendShapeの切り替え、マテリアルの置き換え・プロパティ操作・プロパティ統一などができます。

## Install

現在はUPMからのみインストールできます。

```
https://github.com/lilxyzw/lilycalInventory.git
```

もしgitをインストールしていない場合は[zip](https://github.com/lilxyzw/lilycalInventory/archive/refs/heads/main.zip)をダウンロードし、プロジェクトのPackagesフォルダ配下に配置することでインストールできます。ディレクトリが以下のようになるように配置してください。

- Packages
  - lilycalInventory
    - Editor
    - Prefabs
    - ...

## メニュー生成コンポーネント

|名前|説明|
|-|-|
|LI MenuFolder|メニューのフォルダとなるコンポーネントです。各コンポーネントを整理したい場合に使います。パラメーターは生成されません。|
|LI Prop|付けたオブジェクトをオンオフできるシンプルなコンポーネントです。|
|LI ItemToggler|オブジェクトのオンオフ、BlendShapeの切り替え、マテリアルの置き換え・プロパティ操作を行うコンポーネントです。Boolパラメーターが生成されます。|
|LI CostumeChanger|複数衣装の着替えを想定したコンポーネントで、排他的に動作します。各衣装でItemTogglerと同じような操作ができます。Intパラメーターが生成されます。|
|LI SmoothChanger|アバターの明るさ調整や体型調整を想定したコンポーネントです。無段階でオブジェクトの操作ができます。Floatパラメーターが生成されます。|
|LI AutoDresser|複数衣装の着替えを想定したコンポーネントです。`LI CostumeChanger`同様に動作しますが、こちらは各衣装のルートにつけるだけで動作します。Intパラメーターが生成されます。|

## その他コンポーネント

|名前|説明|
|-|-|
|LI MaterialModifier|アバターのライティングの統一などを想定した、マテリアルの設定を指定したマテリアルに統一するコンポーネントです。|
|LI MaterialOptimizer|マテリアルから自動的に不要なプロパティを削除するコンポーネントです。|

## 使用方法

### オブジェクトのオンオフ

オンオフしたいオブジェクトに`LI Prop`コンポーネントを追加するだけです。

### 複数オブジェクトのオンオフ

1. Hierarchyで右クリックし`Create Empty`でアバター内に新しいオブジェクトを作成
2. そのオブジェクトに`LI ItemToggler`コンポーネントを追加
3. `オブジェクトのオンオフ`にオンオフするオブジェクトを指定

### 複数衣装の切り替え（排他）

1. 各衣装のルートに`LI AutoDresser`コンポーネントを追加
2. （アバター標準衣装などオブジェクトがまとまっていない場合のみ）衣装のメイン部分に`LI AutoDresser`コンポーネントを追加し、`一緒に操作するパラメーター`の`オブジェクトのオンオフ`に衣装の他部分を追加

### アバターの体型調整

1. Hierarchyで右クリックし`Create Empty`でアバター内に新しいオブジェクトを作成
2. そのオブジェクトに`LI SmoothChanger`コンポーネントを追加
3. フレームの+ボタンを押し、フレーム値を0に設定し`BlendShapeの切り替え`に操作するメッシュとBlendShapeを指定（メッシュが未指定であれば全メッシュを操作）
4. フレームの+ボタンを押し、フレーム値を1に設定し3の手順同様に設定

### アバターの明るさ調整

lilToonの場合はPrefabsフォルダ内のLightをアバターに追加するだけで完了です。他シェーダーの場合は手動でプロパティを設定してください。

1. Hierarchyで右クリックし`Create Empty`でアバター内に新しいオブジェクトを作成
2. そのオブジェクトに`LI SmoothChanger`コンポーネントを追加
3. フレームの+ボタンを押し、フレーム値を0に設定し`マテリアルのプロパティ操作`に操作するメッシュとプロパティを指定（メッシュが未指定であれば全メッシュを操作）
4. フレームの+ボタンを押し、フレーム値を1に設定し3の手順同様に設定

### 作成したメニューの整理

1. Hierarchyで右クリックし`Create Empty`でアバター内に新しいオブジェクトを作成
2. そのオブジェクトに`LI MenuFolder`コンポーネントを追加
3. 作成したメニューをこのオブジェクトの配下に入れる（移動できない場合はオーバーライドでこのオブジェクトを指定する）

### lilAvatarModifierからの移行

移行したいアバターのルートやprefabを選択して`Tools/lilycalInventory/Migrate From lilAvatarModifier`を実行すると自動的にスクリプトが置き換えられます。こちらの移行機能は1.0.0リリース時に削除されます。