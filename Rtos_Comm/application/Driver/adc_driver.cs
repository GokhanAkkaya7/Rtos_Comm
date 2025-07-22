using System;
using System.Collections.Generic;
using Rtos_Comm.application.Configuration;
using Rtos_Comm.application.JSON;
using Rtos_Comm;

namespace Rtos_Comm.simulation
{
    public class ADC_Simulator
    {
        private readonly int _resolutionBits;
        private readonly int _bufferLength;
        private readonly List<int> _usedChannels;
        private readonly ushort[] _buffer;
        private int _currentSample = 0;
        private readonly Random _random = new Random();

        private readonly int _minChannel;
        private readonly int _maxChannel;
        private readonly int _blockSize;

        public ADC_Simulator(ADC_Config_Class config)
        {
            _resolutionBits = config.ResolutionBits;
            _bufferLength = config.BufferLength;
            _usedChannels = config.UsedChannels;

            _minChannel = int.MaxValue;
            _maxChannel = int.MinValue;

            foreach (var ch in _usedChannels)
            {
                if (ch < _minChannel) _minChannel = ch;
                if (ch > _maxChannel) _maxChannel = ch;
            }

            _blockSize = (_maxChannel - _minChannel + 1);
            _buffer = new ushort[_bufferLength];
        }

        public AdcData AdcSimulateOneSample(Int32 lower_limit, Int32 upper_limit, int active_ch)
        {
            AdcData arg2sent = new AdcData();

            int baseIndex = _currentSample * _blockSize;
            if (baseIndex + _blockSize > _bufferLength)
            {
                _currentSample = 0;
                baseIndex = 0;
            }

            arg2sent.index = baseIndex;

            foreach (int ch in _usedChannels)
            {
                if (ch == active_ch)
                {
                    int offset = ch - _minChannel;
                    int index = baseIndex + offset;
                    Int32 up_limit = (1 << _resolutionBits);

                    if (upper_limit > up_limit)
                        upper_limit = up_limit;

                    ushort value = (ushort)_random.Next(lower_limit, upper_limit);
                    _buffer[index] = value;
                }
                else
                {
                    int offset = ch - _minChannel;
                    int index = baseIndex + offset;
                    ushort value = (ushort)_random.Next(0, (1 << _resolutionBits));
                    _buffer[index] = value;
                }

            }

            arg2sent.buffer = _buffer;
            _currentSample++;

            return arg2sent;
        }
    }
}
