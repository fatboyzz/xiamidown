从虾米下载音乐吧
=================

## 注意
- 只能下载试听的低品质音乐和对应歌词
- 艺术家、专辑封面等等东西都被忽略了
- 这是个 F# 脚本

## How to use it

### 找到专辑 id
- 进入虾米播放器
- 用 chrome 开发者工具指向左边的某个专辑找到类似：
		
		<div class="collect-item" data-id="********">
	
  data-id 的值就是专辑 id

### Windows
- 需要 .net framework 3.5+
- 需要 F# (vs2010+ or Visual Studio 2010 F# 2.0 Runtime SP1)
- 找到你的专辑 id, 新建一个 input.txt （与 xiamidown.fsx 同目录）并输入 id, 一行一个
- 右键 xiamidown.fsx 选 Run with F# Interactive...
- 下载开始 等待 。。。 下载的 .mp3 与 .lrc 出现在脚本同一目录下
