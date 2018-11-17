# SDDL (Structured Data Definition Language)
SDDL是一个结构化数据定义语言，可根据定义生成各种语言的数据结构和声明。
Fork from [https://github.com/libla/SDDL](https://github.com/libla/SDDL)
此处为工具类，使之使用便捷。具体SDDL文件定义语法异步[上方连接](https://github.com/libla/SDDL)或[README_SDDL](./README_SDDL.md)
 **VS2015编译通过**


CSharpYield主要用于生成对应客户端代码
CSharpAsync主要用于生成异步代码，可用于服务器
***两者皆可自适应修改***

提供Window上**BuildCs.bat**
``` bat
set SLC=bin\Release\SLC.exe         #由SLC项目生成
set Output=.\Protocol.cs            #生成的CS文件储存位置
set NameSpace="Protocol"            #CS文件命名空间
set SDDL=phone.sddl                 #编译的SDDL文件路径
set Yield=bin\Release\CSharpYield   #由CSharpYield项目生成的dll
set Async=bin\Release\CSharpAsync   #由CSharpAsync项目生成的dll
```

