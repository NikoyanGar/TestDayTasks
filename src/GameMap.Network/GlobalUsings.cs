global using System;
global using System.Linq;
global using System.Threading;
global using System.Threading.Tasks;

global using Microsoft.Extensions.DependencyInjection;

global using MagicOnion;
global using MagicOnion.Server;
global using Grpc.Core;

global using GameMap.Shared.Contracts;

// Core feature namespaces
global using GameMap.Core.Features.Objects;
global using GameMap.Core.Features.Regions;
global using GameMap.Core.Features.Surface;

global using GameMap.Core.Storage;
    
// UDP + serialization
global using LiteNetLib;
global using LiteNetLib.Utils;
global using MemoryPack;
