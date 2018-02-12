*【呪】2012/11/07 SME楽曲iTunes Store配信開始*
==============================================

------------------------------------------------------------------------

モーラその後に powered by Lutea project
=======================================

概要
----

moraで購入したmp4ファイルを**再エンコードなしで**iOSデバイス(iPhone,
iPod touch, iPad)で再生できるm4aに変換するツールです

![mora2ios.png](https://raw.githubusercontent.com/gageas/MoraPostprocessor/docs/mora2ios.png)

効果
----

* moraのDRMフリー音源がiOSデバイスで聞けるようになる
* 再エンコードはしないので音質劣化なし
* タグ情報なども全部残る
* さらに、歌詞画像が埋め込まれている場合、アートワークとして追加する

![mora2ios2.png](https://raw.githubusercontent.com/gageas/MoraPostprocessor/docs/mora2ios2.png)

副作用
------

* SONY系デバイス(PS3とかとか?)でジャケット画像が出なくなる?かも?

使い方
------

1. モーラその後に.exeを起動する。
2. moraで購入したmp4ファイルをドラッグ&ドロップする(複数ファイル同時でOK)

ダウンロード
------------

<http://lutea.gageas.com/files/mora2ios.zip>

エラー(NG)が出る場合
--------------------

元ファイルの拡張子が.mp4であることを確認してください。  
元から.m4a拡張子になっているファイルは、.mp4に変更してからD&Dしてください。

参考サイト
----------

[DRMフリーになったmora楽曲を再エンコードなしでiPhone/touchで再生できるようにする方法](http://thousandleaves-project.com/blog/2012/10/drm%E3%83%95%E3%83%AA%E3%83%BC%E3%81%AB%E3%81%AA%E3%81%A3%E3%81%9Fmora%E6%A5%BD%E6%9B%B2%E3%82%92%E5%86%8D%E3%82%A8%E3%83%B3%E3%82%B3%E3%83%BC%E3%83%89%E3%81%AA%E3%81%97%E3%81%A7iphonetouch%E3%81%A7.html)

マニアックな話
--------------

### 原因の解説

moraのmp4にはmoov.meta.ID32(以下ID32)というatom(データ)が含まれています。

このatomが含まれているとiOSデバイスで再生できないことが判明しています。

### このツールの動作

ID32のatomとID32を削除することでゴミになるatomを削除します。

ID32に書かれているのは、MP3等で使用されるID3v2タグで、
moraのmp4は一般的な(iTunes仕様の)タグ情報とID3v2タグとで同じ情報を2箇所に持っています。

ID3v2タグだけに歌詞を画像化したデータが埋め込まれているというケースがあったので、
これは抜き出してアートワーク(moov.udta.meta.ilst.covr)に追加します。

### 何が問題？ 誰が悪い？

iOSのバグ。

ただ、moraのDRM開放まで顕在化しなかった程度の微妙なバグなので、修正されるかというと。。。？

修正されるにしても、それまでの期間iOSで再生出来ないのは不便ですゆえ。

mp4コンテナにどうやってデータを格納すべきかという標準がうんたらという問題は一旦おいておきましょう（これもmp4コンテナの生い立ちからAppleが中心になって決めるべきだと思うのですが）

SONY批判は筋違い。

### ビットレート

iPhoneのCPUで320kbps程度のAACがデコード出来ない訳無いだろ常考

### わざわざ専用ツール作る意味 is 何？

iOSで再生できるようにするだけなら汎用のmp4処理ツールでOK

せっかくなので歌詞画像も残そうかと思ったので作りました

その他
------

ライセンスはNYSL。うまく動かないとかあれば作者まで →
[@gageas](https://twitter.com/gageas)
