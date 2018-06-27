Note that this project implements A bulk token distributor for Relex Life Token (0xd0a024e4b057024f941051dc19220a3bab4b5d45) rather than Relex Token (0x4a42d2c580f83dce404acad18dab26db11a1750e).

# RelexDistributor
This application distributes Relex Token to other wallets from a user's owned wallet.
It asks user to provide full path of a distribution spreadsheet in CSV format that has 2 columns of data seperated by comma.
It treats the first parts of lines seperated by comma as distribution addresses, the second as amount.
By using this application, users need to indicate the address and passphrase where funds will be distributed from.

## Example
An example of the content of a distribution spreadsheet in CSV format is shown as below.
```
0x65513ecd11fd3a5b1fefdcc6a500b025008405a2,100000
0x555ee11fbddc0e49a9bab358a8941ad95ffdb48f,200000.123
0xe08f0bccbca8192620259aa402b29f7b862575d3,300000
0x7ed638621dbb927c947b0ca064abd051cdc93124,12345.654
0x81b7e08f65bdf5648606c89998a9cc8164397647,65432
0x8a91de6b7625a1c0940f4dae084d864c3ce5fe0c,123456
0x65513ecd11fd3a5b1fefdcc6a500b025008405a2,98765.456789
0x555ee11fbddc0e49a9bab358a8941ad95ffdb48f,123
0x9e737dfc1da73c1c0a0c3ca43bb036966003c471,321
0x229115f344a13defba6470d61a3182b60c0d4979,4567.1234
0x63243370ed17a16a1c212b265b4514cbdce23e6f,12
0x23a4cc796859203c5920a3c727c24b2aaf80f407,1.2
```

## Compiling
To compile this application from the source code, you need to install .NET Core 2.1 SDK, which is available at [this link] (https://www.microsoft.com/net/download)

## Running
To run this application, you need to install .NET Core 2.1 Runtime, which is available at [this link] (https://www.microsoft.com/net/download)
After installing .NET Core 2.1 Runtime, run the following command in your terminal (or command prompt),
```
dotnet PATH-TO-RelexDistributor.dll
```