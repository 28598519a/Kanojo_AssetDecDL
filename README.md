# Kanojo_AssetDecDL
用於下載與解密 超次元彼女: 神姫放置の幻想楽園 (Use to download all Kanojo hotfix resources and decrypt files) (日版)

由於資源是動態下載的，意思是如果要正常取得所有的話，必須要遊戲畢業才行 (而且必須手動完整瀏覽過一次所有關卡與介面)

此遊戲的解密使用了他定義的一個叫做cocos2d::FileUtils::s_decodeBuff的函數，而此函數本質上為一個披了馬甲的xxtea解密函數 (IDA裡看起來長的完全不同，但其實本質是相同的)

## ToDo
1. login_protector.dat、protector.dat 這2個檔案紀錄了遊戲資源的副檔名，可以用其來還原副檔名<br>
   (The login_protector.dat and protector.dat files record the file extension of the game resource and can be used to restore the extension)

2. 遊戲的目錄及檔案以這種模式命名3cc2118c-8c7a-f67e-aa85-563ba61829dd，不知道能不能還原，需要研究一下<br>
   (The folders and files of the game are named 3cc2118c-8c7a-f67e-aa85-563ba61829dd in this mode, I don't know whether it can be restored, need to research)

## Usage
1. 下載(Download) : 下載需要選擇hot_file_list.dat
2. Decrypt : 需要選擇資料夾
