import { useState, useEffect, useRef } from 'react'
import './App.css'
import ProductionSunburst from './components/ProductionSunburst'
import dataService from './services/DataService'
import Login from './components/Login'
import * as XLSX from 'xlsx'

function App() {
  // Authentication state
  const [isAuthenticated, setIsAuthenticated] = useState(false)
  const [currentUser, setCurrentUser] = useState(null)

  // Sidebar state
  const [isSidebarCollapsed, setIsSidebarCollapsed] = useState(false)

  const [activeMenu, setActiveMenu] = useState('home')

  // Geri yönlü izlenebilirlik için state'ler
  const [searchType, setSearchType] = useState('')
  const [searchValue, setSearchValue] = useState('')
  const [searchResults, setSearchResults] = useState([])
  const [productionResults, setProductionResults] = useState([])
  const [isSearching, setIsSearching] = useState(false)
  const [activeTab, setActiveTab] = useState('isemri') // Tab kontrolü için

  // Dashboard verileri için state
  const [dashboardData, setDashboardData] = useState({
    totalProduction: 0,
    rawMaterialStock: 0,
    dailyProduction: 0,
    dailyEnjeksiyon: 0,
    dailyMontaj: 0,
    dailySerigrafi: 0
  })
  const [isDashboardLoading, setIsDashboardLoading] = useState(true)
  const [lastUpdated, setLastUpdated] = useState(new Date())
  const [autoRefresh, setAutoRefresh] = useState(true)
  const [quickSearchResults, setQuickSearchResults] = useState([])
  const [isQuickSearching, setIsQuickSearching] = useState(false)
  const [materialConsumptionData, setMaterialConsumptionData] = useState([])
  const [isMaterialLoading, setIsMaterialLoading] = useState(false)
  const [currentMachineType, setCurrentMachineType] = useState('')
  const [montajParts, setMontajParts] = useState({ govdeParts: [], kapakParts: [] })
  const [partMaterialData, setPartMaterialData] = useState({})

  // Sertifikasyon için state'ler
  const [certSearchType, setCertSearchType] = useState('')
  const [certSearchValue, setCertSearchValue] = useState('')
  const [certResults, setCertResults] = useState([])
  const [isCertSearching, setIsCertSearching] = useState(false)
  const [activeCertTab, setActiveCertTab] = useState('kalite-sertifikalari')
  const [ilkNumuneResults, setIlkNumuneResults] = useState([])
  const [isIlkNumuneSearching, setIsIlkNumuneSearching] = useState(false)
  const [isEmriResults, setIsEmriResults] = useState([])
  const [isIsEmriSearching, setIsIsEmriSearching] = useState(false)

  // Kütle Denkliği state'leri
  const [kutleDenkligiResults, setKutleDenkligiResults] = useState([])
  const [isKutleDenkligiSearching, setIsKutleDenkligiSearching] = useState(false)
  const [activeKutleDenkligiTab, setActiveKutleDenkligiTab] = useState('')
  const [kutleDenkligiUretimData, setKutleDenkligiUretimData] = useState({})
  const [kutleDenkligiHammaddeData, setKutleDenkligiHammaddeData] = useState({})
  const [isKutleDenkligiUretimLoading, setIsKutleDenkligiUretimLoading] = useState({})
  const [isKutleDenkligiHammaddeLoading, setIsKutleDenkligiHammaddeLoading] = useState({})

  // Montaj state'leri
  const [montajResults, setMontajResults] = useState([])
  const [isMontajSearching, setIsMontajSearching] = useState(false)
  const [montajUretimData, setMontajUretimData] = useState({})
  const [montajSarfData, setMontajSarfData] = useState({})
  const [isMontajUretimLoading, setIsMontajUretimLoading] = useState({})
  const [isMontajSarfLoading, setIsMontajSarfLoading] = useState({})

  // Filtreleme state'leri
  const [searchResultsFilters, setSearchResultsFilters] = useState({})
  const [productionDetailsFilters, setProductionDetailsFilters] = useState({})
  const [filteredSearchResults, setFilteredSearchResults] = useState([])
  const [filteredProductionDetails, setFilteredProductionDetails] = useState([])

  // Filtreleme fonksiyonları
  const applyFilters = (data, filters) => {
    if (!data || data.length === 0) return data

    return data.filter(item => {
      return Object.entries(filters).every(([column, selectedValues]) => {
        if (!selectedValues || selectedValues.length === 0) return true
        const itemValue = item[column]?.toString() || ''
        return selectedValues.includes(itemValue)
      })
    })
  }

  const getUniqueValues = (data, column) => {
    if (!data || data.length === 0) {
      console.log('getUniqueValues: No data', { data, column })
      return []
    }

    // Tüm property isimlerini kontrol et
    const sampleItem = data[0]
    console.log('getUniqueValues sample item keys:', Object.keys(sampleItem))

    const values = data.map(item => item[column]?.toString() || '').filter(Boolean)
    const uniqueValues = [...new Set(values)].sort()

    console.log('getUniqueValues result:', {
      column,
      dataLength: data.length,
      valuesLength: values.length,
      uniqueLength: uniqueValues.length,
      sample: uniqueValues.slice(0, 3)
    })

    return uniqueValues
  }

  const updateFilter = (filterType, column, selectedValues) => {
    if (filterType === 'searchResults') {
      const newFilters = { ...searchResultsFilters, [column]: selectedValues }
      setSearchResultsFilters(newFilters)
      setFilteredSearchResults(applyFilters(searchResults, newFilters))
    } else if (filterType === 'productionDetails') {
      const newFilters = { ...productionDetailsFilters, [column]: selectedValues }
      setProductionDetailsFilters(newFilters)
      setFilteredProductionDetails(applyFilters(productionResults, newFilters))
    }
  }

  // Veri değiştiğinde filtrelenmiş verileri güncelle
  useEffect(() => {
    setFilteredSearchResults(applyFilters(searchResults, searchResultsFilters))
  }, [searchResults, searchResultsFilters])

  useEffect(() => {
    setFilteredProductionDetails(applyFilters(productionResults, productionDetailsFilters))
  }, [productionResults, productionDetailsFilters])

  // Excel Export fonksiyonu
  const exportToExcel = () => {
    const workbook = XLSX.utils.book_new()

    // İş Emri Bilgileri sheet'i
    if (filteredSearchResults && filteredSearchResults.length > 0) {
      const searchData = filteredSearchResults.map(item => ({
        'İş Emri No': item.ISEMRI_NO || item.isemrI_NO || '-',
        'Stok Kodu': item.STOK_KODU || item.stoK_KODU || '-',
        'Adet': item.ADET || item.adet || 0,
        'Net (kg)': item.NET || item.net || 0,
        'Brüt (kg)': item.BRUT || item.brut || 0,
        'Koli Sayısı': item.KOLI || item.koli || 0,
        'Üretim Tipi': item.URETIM_TIPI || item.uretiM_TIPI || '-'
      }))
      const searchSheet = XLSX.utils.json_to_sheet(searchData)
      XLSX.utils.book_append_sheet(workbook, searchSheet, 'İş Emri Bilgileri')
    }

    // Üretim Bilgileri sheet'i
    if (filteredProductionDetails && filteredProductionDetails.length > 0) {
      const productionData = filteredProductionDetails.map(item => ({
        'İş Emri No': item.ISEMRI_NO || item.isemrI_NO || '-',
        'Stok Kodu': item.STOK_KODU || item.stoK_KODU || '-',
        'Yapı Kodu': item.YAP_KOD || item.yaP_KOD || '-',
        'Seri No': item.SERI_NO || item.serI_NO || '-',
        'Lot No': item.LOT_NO || item.loT_NO || '-',
        'Tarih': item.TARIH || item.tarih || '-',
        'Personel': item.PERSONEL || item.personel || '-',
        'Makina': item.MAKINA || item.makina || '-',
        'Birim Ağırlık': item.B_AGIRLIK || item.b_AGIRLIK || 0,
        'Dara': item.DARA || item.dara || 0,
        'Net': item.NET || item.net || 0,
        'Adet': item.ADET || item.adet || 0
      }))
      const productionSheet = XLSX.utils.json_to_sheet(productionData)
      XLSX.utils.book_append_sheet(workbook, productionSheet, 'Üretim Bilgileri')
    }

    // Kullanılan Malzemeler sheet'i
    if (materialConsumptionData && materialConsumptionData.length > 0) {
      const materialData = materialConsumptionData.map(item => ({
        'İş Emri No': item.VS_SERI_NO || item.vS_SERI_NO || '-',
        'Stok Kodu': item.VS_STOK_KODU || item.vS_STOK_KODU || '-',
        'Stok Adı': item.STOK_ADI || item.stoK_ADI || '-',
        'Hammadde Lot': item.HAMMADDE_LOT || item.hammaddE_LOT || '-',
        'Harcanan Miktar': item.HARCANAN || item.harcanan || 0
      }))
      const materialSheet = XLSX.utils.json_to_sheet(materialData)
      XLSX.utils.book_append_sheet(workbook, materialSheet, 'Kullanılan Malzemeler')
    }

    // Göve ve Kapak Bilgileri sheet'leri (eğer varsa)
    if (currentMachineType === 'M' && partMaterialData) {
      // Gövde parçaları için
      montajParts.govdeParts.forEach((_, index) => {
        const tabName = `govde-${index}`
        const partData = partMaterialData[tabName]
        if (partData && partData.length > 0) {
          const govdeData = partData.map(item => ({
            'İş Emri No': item.isemrI_NO || item.ISEMRI_NO || item.VS_SERI_NO || item.vS_SERI_NO || '-',
            'Stok Kodu': item.VS_STOK_KODU || item.vS_STOK_KODU || '-',
            'Stok Adı': item.STOK_ADI || item.stoK_ADI || '-',
            'Hammadde Lot': item.HAMMADDE_LOT || item.hammaddE_LOT || '-',
            'Harcanan Miktar': item.HARCANAN || item.harcanan || 0
          }))
          const govdeSheet = XLSX.utils.json_to_sheet(govdeData)
          const sheetName = montajParts.govdeParts.length > 1 ? `Gövde ${index + 1}` : 'Gövde'
          XLSX.utils.book_append_sheet(workbook, govdeSheet, sheetName)
        }
      })

      // Kapak parçaları için
      montajParts.kapakParts.forEach((_, index) => {
        const tabName = `kapak-${index}`
        const partData = partMaterialData[tabName]
        if (partData && partData.length > 0) {
          const kapakData = partData.map(item => ({
            'İş Emri No': item.isemrI_NO || item.ISEMRI_NO || item.VS_SERI_NO || item.vS_SERI_NO || '-',
            'Stok Kodu': item.VS_STOK_KODU || item.vS_STOK_KODU || '-',
            'Stok Adı': item.STOK_ADI || item.stoK_ADI || '-',
            'Hammadde Lot': item.HAMMADDE_LOT || item.hammaddE_LOT || '-',
            'Harcanan Miktar': item.HARCANAN || item.harcanan || 0
          }))
          const kapakSheet = XLSX.utils.json_to_sheet(kapakData)
          const sheetName = montajParts.kapakParts.length > 1 ? `Kapak ${index + 1}` : 'Kapak'
          XLSX.utils.book_append_sheet(workbook, kapakSheet, sheetName)
        }
      })
    }

    // Excel dosyasını indir
    const fileName = `Izlenebilirlik_${searchValue}_${new Date().toLocaleDateString('tr-TR').replace(/\./g, '-')}.xlsx`
    XLSX.writeFile(workbook, fileName)
  }

  // FilterDropdown Component
  const FilterDropdown = ({ data, column, filterType, currentFilters, displayName }) => {
    const [isOpen, setIsOpen] = useState(false)
    const [searchTerm, setSearchTerm] = useState('')
    const [selectedValues, setSelectedValues] = useState(currentFilters[column] || [])
    const dropdownRef = useRef(null)

    const uniqueValues = getUniqueValues(data, column)
    const filteredValues = uniqueValues.filter(value =>
      value.toLowerCase().includes(searchTerm.toLowerCase())
    )

    // Debug için
    console.log('FilterDropdown Debug:', {
      column,
      dataLength: data?.length,
      uniqueValues: uniqueValues.slice(0, 5), // İlk 5 değer
      filteredValues: filteredValues.slice(0, 5)
    })

    useEffect(() => {
      const handleClickOutside = (event) => {
        if (dropdownRef.current && !dropdownRef.current.contains(event.target)) {
          setIsOpen(false)
        }
      }
      document.addEventListener('mousedown', handleClickOutside)
      return () => document.removeEventListener('mousedown', handleClickOutside)
    }, [])

    const handleValueToggle = (value) => {
      const newSelected = selectedValues.includes(value)
        ? selectedValues.filter(v => v !== value)
        : [...selectedValues, value]
      setSelectedValues(newSelected)
    }

    const handleSelectAll = () => {
      setSelectedValues(filteredValues)
    }

    const handleClearAll = () => {
      setSelectedValues([])
    }

    const handleApply = () => {
      updateFilter(filterType, column, selectedValues)
      setIsOpen(false)
    }

    const hasActiveFilter = currentFilters[column] && currentFilters[column].length > 0

    return (
      <div className={`filter-header ${isOpen ? 'open' : ''}`} ref={dropdownRef}>
        <span>{displayName || column.replace(/([A-Z])/g, ' $1').replace(/^./, str => str.toUpperCase())}</span>
        <span
          className={`filter-icon ${hasActiveFilter ? 'active' : ''}`}
          onClick={() => setIsOpen(!isOpen)}
          style={{ color: hasActiveFilter ? '#007bff' : '#666' }}
        >
          ▼
        </span>

        {isOpen && (
          <div className="filter-dropdown">
            <input
              type="text"
              className="filter-search"
              placeholder="Ara..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
            />

            <div className="filter-options">
              {filteredValues.map(value => (
                <div key={value} className="filter-option">
                  <input
                    type="checkbox"
                    checked={selectedValues.includes(value)}
                    onChange={() => handleValueToggle(value)}
                  />
                  <span>{value}</span>
                </div>
              ))}
            </div>

            <div className="filter-actions">
              <button className="filter-btn" onClick={handleSelectAll}>
                Tümünü Seç
              </button>
              <button className="filter-btn" onClick={handleClearAll}>
                Temizle
              </button>
              <button className="filter-btn primary" onClick={handleApply}>
                Uygula
              </button>
            </div>
          </div>
        )}
      </div>
    )
  }

  // Dashboard verilerini yükle (DataService kullanarak)
  const loadDashboardData = async (forceRefresh = false) => {
    console.log('loadDashboardData çağrıldı, forceRefresh:', forceRefresh)
    try {
      // Yükleme durumunu başlat
      setIsDashboardLoading(true)
      console.log('Dashboard loading state: true')

      // Timeout ile veri yükleme
      const loadingTimeout = setTimeout(() => {
        // 15 saniye sonra hala yükleme devam ediyorsa, loading state'i kapat
        // ve kullanıcıya bilgi ver
        console.log('Dashboard veri yükleme zaman aşımı - 15 saniye')
        setIsDashboardLoading(false)
      }, 15000)

      // Veri yükleme
      console.log('DataService.getDashboardData çağrılıyor...')
      const data = await dataService.getDashboardData(forceRefresh)
      console.log('DataService\'den alınan veri:', data)

      // Timeout'u temizle
      clearTimeout(loadingTimeout)

      // State'leri güncelle
      setDashboardData(data)
      setLastUpdated(data.lastUpdated)
      console.log('Dashboard state güncellendi')
    } catch (error) {
      console.error('Dashboard veri yükleme hatası:', error)
    } finally {
      setIsDashboardLoading(false)
      console.log('Dashboard loading state: false')
    }
  }

  // Authentication functions
  const handleLogin = (username) => {
    setIsAuthenticated(true)
    setCurrentUser(username)
    // Store in localStorage for persistence
    localStorage.setItem('isAuthenticated', 'true')
    localStorage.setItem('currentUser', username)
  }

  const handleLogout = () => {
    setIsAuthenticated(false)
    setCurrentUser(null)
    // Clear localStorage
    localStorage.removeItem('isAuthenticated')
    localStorage.removeItem('currentUser')
  }

  const toggleSidebar = () => {
    setIsSidebarCollapsed(!isSidebarCollapsed)
  }

  // Check authentication on app load
  useEffect(() => {
    const storedAuth = localStorage.getItem('isAuthenticated')
    const storedUser = localStorage.getItem('currentUser')

    if (storedAuth === 'true' && storedUser) {
      setIsAuthenticated(true)
      setCurrentUser(storedUser)
    }
  }, [])

  // Authentication değiştiğinde dashboard verilerini yükle
  useEffect(() => {
    if (isAuthenticated) {
      console.log('Authentication başarılı - Dashboard verileri yükleniyor...')
      loadDashboardData()
    }
  }, [isAuthenticated])

  // Component mount olduğunda DataService'i ayarla
  useEffect(() => {
    // DataService'den veri güncellemelerini dinle
    const unsubscribe = dataService.subscribe('dashboard', (data) => {
      console.log('Dashboard verileri DataService\'den alındı:', data)
      setDashboardData(data)
      setLastUpdated(data.lastUpdated)
    })

    // Otomatik yenileme durumunu senkronize et
    dataService.setAutoRefresh(autoRefresh)

    // Cleanup
    return () => {
      unsubscribe()
    }
  }, [])

  // Otomatik yenileme durumu değiştiğinde DataService'i güncelle
  useEffect(() => {
    if (isAuthenticated) {
      dataService.setAutoRefresh(autoRefresh)
    }
  }, [autoRefresh, isAuthenticated])

  // Hızlı arama fonksiyonu (tıklanabilir hücreler için)
  const handleQuickSearch = async (searchType, searchValue) => {
    if (!searchValue || !searchType) return

    setIsQuickSearching(true)

    try {
      const response = await fetch('http://localhost:5022/api/BackwardTraceability/quick-search', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          searchType: searchType,
          searchValue: searchValue
        })
      })

      if (response.ok) {
        const result = await response.json()
        if (result.success) {
          setQuickSearchResults(result.data)
          setActiveTab('quick') // Otomatik olarak detay tab'ına geç
          console.log(`Hızlı arama tamamlandı: ${result.count} kayıt bulundu`)
        } else {
          console.error('Hızlı arama başarısız:', result.message)
          setQuickSearchResults([])
        }
      } else {
        console.error('Hızlı arama API hatası')
        setQuickSearchResults([])
      }
    } catch (error) {
      console.error('Hızlı arama çağrısı hatası:', error)
      setQuickSearchResults([])
    } finally {
      setIsQuickSearching(false)
    }
  }

  // Tıklanabilir hücre bileşeni
  const ClickableCell = ({ value, searchType, className = "" }) => {
    if (!value || value === '-') {
      return <td className={className}>{value || '-'}</td>
    }

    return (
      <td className={`${className} clickable-cell`}>
        <span
          className="clickable-value"
          onClick={() => handleQuickSearch(searchType, value)}
          title={`${searchType === 'isemri_no' ? 'İş Emri' : searchType === 'seri_no' ? 'Seri' : 'Lot'} detaylarını görüntüle`}
        >
          {value}
        </span>
      </td>
    )
  }

  // Üretim bilgileri yükleme fonksiyonu
  const loadProductionDetails = async () => {
    if (!searchValue.trim()) return
    
    try {
      const response = await fetch('http://localhost:5022/api/BackwardTraceability/production-details', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          searchType: searchType,
          searchValue: searchValue
        })
      })
      
      if (response.ok) {
        const data = await response.json()
        setProductionResults(data)
      } else {
        console.error('Üretim bilgileri yüklenemedi')
        setProductionResults([])
      }
    } catch (error) {
      console.error('Üretim bilgileri API çağrısı hatası:', error)
      setProductionResults([])
    }
  }

  // Arama fonksiyonu
  const handleSearch = async () => {
    if (!searchType || !searchValue.trim()) {
      alert('Lütfen arama tipini seçin ve değer girin!')
      return
    }

    setIsSearching(true)

    try {
      const response = await fetch('http://localhost:5022/api/BackwardTraceability/search', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          searchType: searchType,
          searchValue: searchValue
        })
      })

      if (response.ok) {
        const data = await response.json()
        setSearchResults(data)
        setActiveTab('isemri') // Arama yapıldığında ilk tab'ı aktif yap

        // Makine tipini kontrol et (tab'lar için)
        const machineTypeResult = await checkMachineType(searchValue.trim())
        if (machineTypeResult && machineTypeResult.success) {
          setCurrentMachineType(machineTypeResult.machineType)

          // Montaj makinesi ise parçaları da yükle
          if (machineTypeResult.machineType === 'M') {
            await loadMontajParts(searchValue.trim())
          }
        } else {
          setCurrentMachineType('')
        }
      } else {
        alert('Arama sırasında hata oluştu!')
        setSearchResults([])
      }
    } catch (error) {
      console.error('API çağrısı hatası:', error)
      alert('Sunucuya bağlanırken hata oluştu!')
      setSearchResults([])
    } finally {
      setIsSearching(false)
    }
  }

  // Arama tipini değiştirdiğinde değeri temizle
  const handleSearchTypeChange = (type) => {
    setSearchType(type)
    setSearchValue('')
    setSearchResults([])
  }

  // Arama değerini temizle
  const clearSearch = () => {
    setSearchType('')
    setSearchValue('')
    setSearchResults([])
  }

  // Makine tipi kontrol fonksiyonu
  const checkMachineType = async (searchValue) => {
    try {
      console.log('Makine tipi kontrol ediliyor:', searchValue)
      const response = await fetch('http://localhost:5022/api/BackwardTraceability/machine-type', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          searchValue: searchValue
        })
      })

      if (response.ok) {
        const data = await response.json()
        console.log('Makine tipi sonucu:', data)
        return data
      } else {
        const errorData = await response.json()
        console.error('Makine tipi hatası:', errorData)
        return null
      }
    } catch (error) {
      console.error('Makine tipi API hatası:', error)
      return null
    }
  }

  // Montaj parçalarını yükle
  const loadMontajParts = async (searchValue) => {
    try {
      console.log('Montaj parçaları yükleniyor:', searchValue)

      const response = await fetch('http://localhost:5022/api/BackwardTraceability/montaj-parts', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          searchValue: searchValue
        })
      })

      if (response.ok) {
        const data = await response.json()
        console.log('Montaj parçaları sonucu:', data)
        setMontajParts({
          govdeParts: data.govdeParts || [],
          kapakParts: data.kapakParts || []
        })
        return data
      } else {
        const errorData = await response.json()
        console.error('Montaj parçaları hatası:', errorData)
        setMontajParts({ govdeParts: [], kapakParts: [] })
        return null
      }
    } catch (error) {
      console.error('Montaj parçaları API hatası:', error)
      setMontajParts({ govdeParts: [], kapakParts: [] })
      return null
    }
  }

  // Sarf malzeme verilerini yükle
  const loadMaterialConsumption = async (searchValue, machineType) => {
    try {
      setIsMaterialLoading(true)
      console.log('Sarf malzeme verileri yükleniyor:', { searchValue, machineType })

      const response = await fetch('http://localhost:5022/api/BackwardTraceability/material-consumption', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          searchValue: searchValue,
          machineType: machineType
        })
      })

      if (response.ok) {
        const data = await response.json()
        console.log('Sarf malzeme sonucu:', data)
        setMaterialConsumptionData(data.data || [])
        return data
      } else {
        const errorData = await response.json()
        console.error('Sarf malzeme hatası:', errorData)
        setMaterialConsumptionData([])
        return null
      }
    } catch (error) {
      console.error('Sarf malzeme API hatası:', error)
      setMaterialConsumptionData([])
      return null
    } finally {
      setIsMaterialLoading(false)
    }
  }

  // Parça için sarf malzeme verilerini yükle
  const loadPartMaterialConsumption = async (isemriNo, partType) => {
    try {
      console.log('Parça sarf malzeme verileri yükleniyor:', { isemriNo, partType })

      const response = await fetch('http://localhost:5022/api/BackwardTraceability/material-consumption', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          searchValue: isemriNo,
          machineType: 'MONTAJ_PART' // Montaj parçası için özel sorgu
        })
      })

      if (response.ok) {
        const data = await response.json()
        console.log('Parça sarf malzeme sonucu:', data)
        setPartMaterialData(prev => ({
          ...prev,
          [partType]: data.data || []
        }))
        return data
      } else {
        const errorData = await response.json()
        console.error('Parça sarf malzeme hatası:', errorData)
        return null
      }
    } catch (error) {
      console.error('Parça sarf malzeme API hatası:', error)
      return null
    }
  }

  // Sertifikasyon arama fonksiyonu - Dört tab için de aynı anda arama yapar
  const handleCertSearch = async () => {
    if (!certSearchValue.trim() || !certSearchType) return

    // Beş tab için de arama yap
    setIsCertSearching(true)
    setIsIlkNumuneSearching(true)
    setIsIsEmriSearching(true)
    setIsKutleDenkligiSearching(true)
    setIsMontajSearching(true)

    try {
      console.log('Sertifikasyon araması başlatılıyor:', { certSearchType, certSearchValue })

      // Kalite Sertifikaları araması
      const kalitePromise = fetch('http://localhost:5022/api/Certification/search', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          searchType: certSearchType,
          searchValue: certSearchValue.trim()
        })
      })

      // İlk Numune araması
      const ilkNumunePromise = fetch('http://localhost:5022/api/Certification/search-ilk-numune', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          searchType: certSearchType,
          searchValue: certSearchValue.trim()
        })
      })

      // İş Emri araması
      const isEmriPromise = fetch('http://localhost:5022/api/Certification/search-is-emri', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          searchType: certSearchType,
          searchValue: certSearchValue.trim()
        })
      })

      // Kütle Denkliği araması
      const kutleDenkligiPromise = fetch('http://localhost:5022/api/Certification/search-kutle-denkligi', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          searchType: certSearchType,
          searchValue: certSearchValue.trim()
        })
      })

      // Montaj araması
      const montajPromise = fetch('http://localhost:5022/api/Certification/search-montaj', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          searchType: certSearchType,
          searchValue: certSearchValue.trim()
        })
      })

      // Beş aramayı paralel olarak çalıştır
      const [kaliteResponse, ilkNumuneResponse, isEmriResponse, kutleDenkligiResponse, montajResponse] = await Promise.all([kalitePromise, ilkNumunePromise, isEmriPromise, kutleDenkligiPromise, montajPromise])

      // Kalite Sertifikaları sonuçlarını işle
      if (kaliteResponse.ok) {
        const kaliteData = await kaliteResponse.json()
        console.log('Kalite Sertifikaları sonucu:', kaliteData)
        setCertResults(kaliteData.results || [])
      } else {
        const kaliteErrorData = await kaliteResponse.json()
        console.error('Kalite Sertifikaları arama hatası:', kaliteErrorData)
        setCertResults([])
      }

      // İlk Numune sonuçlarını işle
      if (ilkNumuneResponse.ok) {
        const ilkNumuneData = await ilkNumuneResponse.json()
        console.log('İlk Numune sonucu:', ilkNumuneData)
        setIlkNumuneResults(ilkNumuneData.results || [])
      } else {
        const ilkNumuneErrorData = await ilkNumuneResponse.json()
        console.error('İlk Numune arama hatası:', ilkNumuneErrorData)
        setIlkNumuneResults([])
      }

      // İş Emri sonuçlarını işle
      if (isEmriResponse.ok) {
        const isEmriData = await isEmriResponse.json()
        console.log('İş Emri sonucu:', isEmriData)
        setIsEmriResults(isEmriData.results || [])
      } else {
        const isEmriErrorData = await isEmriResponse.json()
        console.error('İş Emri arama hatası:', isEmriErrorData)
        setIsEmriResults([])
      }

      // Kütle Denkliği sonuçlarını işle
      let kutleDenkligiData = null
      if (kutleDenkligiResponse.ok) {
        kutleDenkligiData = await kutleDenkligiResponse.json()
        console.log('Kütle Denkliği sonucu:', kutleDenkligiData)
        setKutleDenkligiResults(kutleDenkligiData.results || [])
      } else {
        const kutleDenkligiErrorData = await kutleDenkligiResponse.json()
        console.error('Kütle Denkliği arama hatası:', kutleDenkligiErrorData)
        setKutleDenkligiResults([])
      }

      // Montaj sonuçlarını işle
      let montajData = null
      if (montajResponse.ok) {
        montajData = await montajResponse.json()
        console.log('Montaj sonucu:', montajData)
        setMontajResults(montajData.results || [])
      } else {
        const montajErrorData = await montajResponse.json()
        console.error('Montaj arama hatası:', montajErrorData)
        setMontajResults([])
      }

      // İlk tab'ı otomatik olarak aktif yap - önce montaj, sonra kütle denkliği
      if (montajData && montajData.results && montajData.results.length > 0) {
        setActiveKutleDenkligiTab(montajData.results[0].tabBaslik)
      } else if (kutleDenkligiData && kutleDenkligiData.results && kutleDenkligiData.results.length > 0) {
        setActiveKutleDenkligiTab(kutleDenkligiData.results[0].tabBaslik)
      }

    } catch (error) {
      console.error('Sertifikasyon API hatası:', error)
      setCertResults([])
      setIlkNumuneResults([])
      setIsEmriResults([])
      setKutleDenkligiResults([])
      setMontajResults([])
    } finally {
      setIsCertSearching(false)
      setIsIlkNumuneSearching(false)
      setIsIsEmriSearching(false)
      setIsKutleDenkligiSearching(false)
      setIsMontajSearching(false)
    }
  }

  // Kütle Denkliği üretim verilerini yükle
  const loadKutleDenkligiUretimData = async (isemriNo, tabBaslik) => {
    try {
      setIsKutleDenkligiUretimLoading(prev => ({ ...prev, [tabBaslik]: true }))

      const response = await fetch('http://localhost:5022/api/Certification/kutle-denkligi-uretim', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          isemriNo: isemriNo
        })
      })

      if (response.ok) {
        const data = await response.json()
        setKutleDenkligiUretimData(prev => ({ ...prev, [tabBaslik]: data.results || [] }))
      } else {
        console.error('Kütle Denkliği üretim verileri yüklenemedi')
        setKutleDenkligiUretimData(prev => ({ ...prev, [tabBaslik]: [] }))
      }
    } catch (error) {
      console.error('Kütle Denkliği üretim verileri API hatası:', error)
      setKutleDenkligiUretimData(prev => ({ ...prev, [tabBaslik]: [] }))
    } finally {
      setIsKutleDenkligiUretimLoading(prev => ({ ...prev, [tabBaslik]: false }))
    }
  }

  // Kütle Denkliği hammadde verilerini yükle
  const loadKutleDenkligiHammaddeData = async (isemriNo, tabBaslik) => {
    try {
      setIsKutleDenkligiHammaddeLoading(prev => ({ ...prev, [tabBaslik]: true }))

      const response = await fetch('http://localhost:5022/api/Certification/kutle-denkligi-hammadde', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          isemriNo: isemriNo
        })
      })

      if (response.ok) {
        const data = await response.json()
        setKutleDenkligiHammaddeData(prev => ({ ...prev, [tabBaslik]: data.results || [] }))
      } else {
        console.error('Kütle Denkliği hammadde verileri yüklenemedi')
        setKutleDenkligiHammaddeData(prev => ({ ...prev, [tabBaslik]: [] }))
      }
    } catch (error) {
      console.error('Kütle Denkliği hammadde verileri API hatası:', error)
      setKutleDenkligiHammaddeData(prev => ({ ...prev, [tabBaslik]: [] }))
    } finally {
      setIsKutleDenkligiHammaddeLoading(prev => ({ ...prev, [tabBaslik]: false }))
    }
  }

  // Montaj üretim verilerini yükle
  const loadMontajUretimData = async (isemriNo, tabBaslik) => {
    try {
      setIsMontajUretimLoading(prev => ({ ...prev, [tabBaslik]: true }))

      const response = await fetch('http://localhost:5022/api/Certification/montaj-uretim', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          isemriNo: isemriNo
        })
      })

      if (response.ok) {
        const data = await response.json()
        setMontajUretimData(prev => ({ ...prev, [tabBaslik]: data.results || [] }))
      } else {
        console.error('Montaj üretim verileri yüklenemedi')
        setMontajUretimData(prev => ({ ...prev, [tabBaslik]: [] }))
      }
    } catch (error) {
      console.error('Montaj üretim verileri API hatası:', error)
      setMontajUretimData(prev => ({ ...prev, [tabBaslik]: [] }))
    } finally {
      setIsMontajUretimLoading(prev => ({ ...prev, [tabBaslik]: false }))
    }
  }

  // Montaj sarf verilerini yükle
  const loadMontajSarfData = async (isemriNo, tabBaslik) => {
    try {
      setIsMontajSarfLoading(prev => ({ ...prev, [tabBaslik]: true }))

      const response = await fetch('http://localhost:5022/api/Certification/montaj-sarf', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          isemriNo: isemriNo
        })
      })

      if (response.ok) {
        const data = await response.json()
        setMontajSarfData(prev => ({ ...prev, [tabBaslik]: data.results || [] }))
      } else {
        console.error('Montaj sarf verileri yüklenemedi')
        setMontajSarfData(prev => ({ ...prev, [tabBaslik]: [] }))
      }
    } catch (error) {
      console.error('Montaj sarf verileri API hatası:', error)
      setMontajSarfData(prev => ({ ...prev, [tabBaslik]: [] }))
    } finally {
      setIsMontajSarfLoading(prev => ({ ...prev, [tabBaslik]: false }))
    }
  }

  // Kütle Denkliği tab değiştiğinde verileri yükle
  useEffect(() => {
    if (activeKutleDenkligiTab && kutleDenkligiResults.length > 0) {
      const currentItem = kutleDenkligiResults.find(item => item.tabBaslik === activeKutleDenkligiTab)
      if (currentItem && currentItem.isemriNo) {
        // Eğer veriler henüz yüklenmemişse yükle
        if (!kutleDenkligiUretimData[activeKutleDenkligiTab]) {
          loadKutleDenkligiUretimData(currentItem.isemriNo, activeKutleDenkligiTab)
        }
        if (!kutleDenkligiHammaddeData[activeKutleDenkligiTab]) {
          loadKutleDenkligiHammaddeData(currentItem.isemriNo, activeKutleDenkligiTab)
        }
      }
    }
  }, [activeKutleDenkligiTab, kutleDenkligiResults])

  // Montaj tab değiştiğinde verileri yükle
  useEffect(() => {
    if (activeKutleDenkligiTab && activeKutleDenkligiTab.includes('Montaj') && montajResults.length > 0) {
      const currentItem = montajResults.find(item => item.tabBaslik === activeKutleDenkligiTab)
      if (currentItem && currentItem.isemriNo) {
        // Eğer veriler henüz yüklenmemişse yükle
        if (!montajUretimData[activeKutleDenkligiTab]) {
          loadMontajUretimData(currentItem.isemriNo, activeKutleDenkligiTab)
        }
        if (!montajSarfData[activeKutleDenkligiTab]) {
          loadMontajSarfData(currentItem.isemriNo, activeKutleDenkligiTab)
        }
      }
    }
  }, [activeKutleDenkligiTab, montajResults])

  // PDF önizleme fonksiyonu
  const previewPDF = async (analizeNumber) => {
    try {
      const url = 'http://localhost:5022/api/Certification/preview-pdf'
      console.log('PDF önizleme isteği:', analizeNumber)
      console.log('Request URL:', url)
      const response = await fetch(url, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          AnalizeNumber: analizeNumber
        })
      })

      console.log('PDF önizleme response status:', response.status)
      console.log('PDF response content-type:', response.headers.get('content-type'))

      if (response.ok) {
        const contentType = response.headers.get('content-type')

        if (contentType && contentType.includes('application/pdf')) {
          // PDF dosyası bulundu
          const blob = await response.blob()
          const url = window.URL.createObjectURL(blob)
          window.open(url, '_blank')
        } else {
          // JSON response (hata mesajı)
          const responseData = await response.json()
          console.log('PDF response data:', responseData)
          alert(`PDF hatası: ${responseData.message}`)
        }
      } else {
        console.error('PDF önizleme hatası:', response.statusText)
        const errorText = await response.text()
        console.error('PDF önizleme error response:', errorText)
        alert('PDF dosyası bulunamadı!')
      }
    } catch (error) {
      console.error('PDF önizleme hatası:', error)
      alert('PDF önizleme sırasında hata oluştu!')
    }
  }

  // PDF indirme fonksiyonu
  const downloadPDF = async (analizeNumber) => {
    try {
      const response = await fetch('http://localhost:5022/api/Certification/download-pdf', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          AnalizeNumber: analizeNumber
        })
      })

      if (response.ok) {
        const contentType = response.headers.get('content-type')

        if (contentType && contentType.includes('application/pdf')) {
          // PDF dosyası bulundu
          const blob = await response.blob()
          const url = window.URL.createObjectURL(blob)
          const a = document.createElement('a')
          a.href = url
          a.download = `${analizeNumber}.pdf`
          document.body.appendChild(a)
          a.click()
          document.body.removeChild(a)
          window.URL.revokeObjectURL(url)
        } else {
          // JSON response (hata mesajı)
          const responseData = await response.json()
          alert(`PDF hatası: ${responseData.message}`)
        }
      } else {
        alert('PDF dosyası bulunamadı!')
      }
    } catch (error) {
      console.error('PDF indirme hatası:', error)
      alert('PDF indirme sırasında hata oluştu!')
    }
  }

  // İlk Numune PDF önizleme fonksiyonu
  const previewIlkNumunePDF = async (analizeNumber) => {
    try {
      const url = 'http://localhost:5022/api/Certification/preview-ilk-numune-pdf'
      console.log('İlk Numune PDF önizleme isteği:', analizeNumber)
      const response = await fetch(url, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          AnalizeNumber: analizeNumber
        })
      })

      if (response.ok) {
        const blob = await response.blob()
        const pdfUrl = URL.createObjectURL(blob)
        window.open(pdfUrl, '_blank')
      } else {
        const errorData = await response.json()
        alert(`PDF hatası: ${errorData.message}`)
      }
    } catch (error) {
      console.error('İlk Numune PDF önizleme hatası:', error)
      alert('PDF önizleme sırasında hata oluştu!')
    }
  }

  // İlk Numune PDF indirme fonksiyonu
  const downloadIlkNumunePDF = async (analizeNumber) => {
    try {
      const response = await fetch('http://localhost:5022/api/Certification/download-ilk-numune-pdf', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          AnalizeNumber: analizeNumber
        })
      })

      if (response.ok) {
        const contentType = response.headers.get('content-type')

        if (contentType && contentType.includes('application/pdf')) {
          const blob = await response.blob()
          const url = window.URL.createObjectURL(blob)
          const a = document.createElement('a')
          a.href = url
          a.download = `${analizeNumber}.pdf`
          document.body.appendChild(a)
          a.click()
          window.URL.revokeObjectURL(url)
          document.body.removeChild(a)
        } else {
          const responseData = await response.json()
          alert(`PDF hatası: ${responseData.message}`)
        }
      } else {
        alert('PDF dosyası bulunamadı!')
      }
    } catch (error) {
      console.error('İlk Numune PDF indirme hatası:', error)
      alert('PDF indirme sırasında hata oluştu!')
    }
  }

  // Tab değiştirme fonksiyonu
  const handleTabChange = async (tabName) => {
    setActiveTab(tabName)

    // Üretim bilgileri tab'ına geçildiğinde veri yükle
    if (tabName === 'uretim' && searchValue.trim()) {
      await loadProductionDetails()
    }

    // Sarf malzemeler tab'ına geçildiğinde makine tipini kontrol et ve verileri yükle
    if (tabName === 'sarf' && searchValue.trim()) {
      const machineTypeResult = await checkMachineType(searchValue.trim())
      if (machineTypeResult && machineTypeResult.success) {
        console.log(`Makine tipi: ${machineTypeResult.machineType} (${machineTypeResult.machineTypeName})`)
        setCurrentMachineType(machineTypeResult.machineType)
        // Makine tipine göre sarf malzeme verilerini yükle
        await loadMaterialConsumption(searchValue.trim(), machineTypeResult.machineType)
      } else {
        console.log('Makine tipi belirlenemedi')
        setCurrentMachineType('')
        setMaterialConsumptionData([])
      }
    }

    // Gövde tab'ları için
    if (tabName.startsWith('govde-') && searchValue.trim()) {
      const govdeIndex = parseInt(tabName.split('-')[1])
      const govdePart = montajParts.govdeParts[govdeIndex]
      if (govdePart) {
        console.log('Gövde tab\'ı seçildi:', govdePart)
        // Backend'den gelen property ismi: isemrI_NO
        const isemriNo = govdePart.isemrI_NO || govdePart.ISEMRI_NO
        if (isemriNo) {
          await loadPartMaterialConsumption(isemriNo, tabName)
        } else {
          console.error('Gövde parçası için İş Emri No bulunamadı:', govdePart)
        }
      }
    }

    // Kapak tab'ları için
    if (tabName.startsWith('kapak-') && searchValue.trim()) {
      const kapakIndex = parseInt(tabName.split('-')[1])
      const kapakPart = montajParts.kapakParts[kapakIndex]
      if (kapakPart) {
        console.log('Kapak tab\'ı seçildi:', kapakPart)
        // Backend'den gelen property ismi: isemrI_NO
        const isemriNo = kapakPart.isemrI_NO || kapakPart.ISEMRI_NO
        if (isemriNo) {
          await loadPartMaterialConsumption(isemriNo, tabName)
        } else {
          console.error('Kapak parçası için İş Emri No bulunamadı:', kapakPart)
        }
      }
    }
  }

  // Aktif tab'a göre tablo içeriğini render et
  const renderTabContent = () => {
    switch (activeTab) {
      case 'isemri':
        return (
          <table className="results-table">
            <thead>
              <tr>
                <th>
                  <FilterDropdown
                    data={searchResults}
                    column="isemrI_NO"
                    filterType="searchResults"
                    currentFilters={searchResultsFilters}
                    displayName="İş Emri No"
                  />
                </th>
                <th>
                  <FilterDropdown
                    data={searchResults}
                    column="stoK_KODU"
                    filterType="searchResults"
                    currentFilters={searchResultsFilters}
                    displayName="Stok Kodu"
                  />
                </th>
                <th>Adet</th>
                <th>Net (kg)</th>
                <th>Brüt (kg)</th>
                <th>Koli Sayısı</th>
                <th>
                  <FilterDropdown
                    data={searchResults}
                    column="uretiM_TIPI"
                    filterType="searchResults"
                    currentFilters={searchResultsFilters}
                    displayName="Üretim Tipi"
                  />
                </th>
              </tr>
            </thead>
            <tbody>
              {(filteredSearchResults.length > 0 ? filteredSearchResults : searchResults).map((item, index) => (
                <tr key={index}>
                  <ClickableCell
                    value={item.ISEMRI_NO || item.isemrI_NO || '-'}
                    searchType="isemri_no"
                  />
                  <td>{item.STOK_KODU || item.stoK_KODU || '-'}</td>
                  <td>{(item.ADET || item.adet || 0).toLocaleString()}</td>
                  <td>{(item.NET || item.net || 0).toFixed(1)}</td>
                  <td>{(item.BRUT || item.brut || 0).toFixed(1)}</td>
                  <td>{item.KOLI || item.koli || 0}</td>
                  <td>{item.URETIM_TIPI || item.uretiM_TIPI || '-'}</td>
                </tr>
              ))}
              {searchResults.length > 0 && (
                <tr className="total-row">
                  <td colSpan="2"><strong>TOPLAM</strong></td>
                  <td><strong>
                    {(filteredSearchResults.length > 0 ? filteredSearchResults : searchResults)
                      .reduce((total, item) => total + (item.ADET || item.adet || 0), 0)
                      .toLocaleString()
                    }
                  </strong></td>
                  <td><strong>
                    {(filteredSearchResults.length > 0 ? filteredSearchResults : searchResults)
                      .reduce((total, item) => total + (item.NET || item.net || 0), 0)
                      .toFixed(1)
                    }
                  </strong></td>
                  <td><strong>
                    {(filteredSearchResults.length > 0 ? filteredSearchResults : searchResults)
                      .reduce((total, item) => total + (item.BRUT || item.brut || 0), 0)
                      .toFixed(1)
                    }
                  </strong></td>
                  <td><strong>
                    {(filteredSearchResults.length > 0 ? filteredSearchResults : searchResults)
                      .reduce((total, item) => total + (item.KOLI || item.koli || 0), 0)
                      .toLocaleString()
                    }
                  </strong></td>
                  <td></td>
                </tr>
              )}
            </tbody>
          </table>
        )
      case 'uretim':
        return (
          <table className="results-table">
            <thead>
              <tr>
                <th>
                  <FilterDropdown
                    data={productionResults}
                    column="isemrI_NO"
                    filterType="productionDetails"
                    currentFilters={productionDetailsFilters}
                    displayName="İş Emri No"
                  />
                </th>
                <th>
                  <FilterDropdown
                    data={productionResults}
                    column="stoK_KODU"
                    filterType="productionDetails"
                    currentFilters={productionDetailsFilters}
                    displayName="Stok Kodu"
                  />
                </th>
                <th>
                  <FilterDropdown
                    data={productionResults}
                    column="yaP_KOD"
                    filterType="productionDetails"
                    currentFilters={productionDetailsFilters}
                    displayName="Yapı Kodu"
                  />
                </th>
                <th>
                  <FilterDropdown
                    data={productionResults}
                    column="serI_NO"
                    filterType="productionDetails"
                    currentFilters={productionDetailsFilters}
                    displayName="Seri No"
                  />
                </th>
                <th>
                  <FilterDropdown
                    data={productionResults}
                    column="loT_NO"
                    filterType="productionDetails"
                    currentFilters={productionDetailsFilters}
                    displayName="Lot No"
                  />
                </th>
                <th>
                  <FilterDropdown
                    data={productionResults}
                    column="tarih"
                    filterType="productionDetails"
                    currentFilters={productionDetailsFilters}
                    displayName="Tarih"
                  />
                </th>
                <th>
                  <FilterDropdown
                    data={productionResults}
                    column="personel"
                    filterType="productionDetails"
                    currentFilters={productionDetailsFilters}
                    displayName="Personel"
                  />
                </th>
                <th>
                  <FilterDropdown
                    data={productionResults}
                    column="makina"
                    filterType="productionDetails"
                    currentFilters={productionDetailsFilters}
                    displayName="Makina"
                  />
                </th>
                <th>Birim Ağırlık</th>
                <th>Dara</th>
                <th>Net</th>
                <th>Adet</th>
              </tr>
            </thead>
            <tbody>
              {productionResults.length > 0 ? (filteredProductionDetails.length > 0 ? filteredProductionDetails : productionResults).map((item, index) => (
                <tr key={index}>
                  <ClickableCell
                    value={item.ISEMRI_NO || item.isemrI_NO || '-'}
                    searchType="isemri_no"
                  />
                  <td>{item.STOK_KODU || item.stoK_KODU || '-'}</td>
                  <td>{item.YAP_KOD || item.yaP_KOD || '-'}</td>
                  <ClickableCell
                    value={item.SERI_NO || item.serI_NO || '-'}
                    searchType="seri_no"
                  />
                  <ClickableCell
                    value={item.LOT_NO || item.loT_NO || '-'}
                    searchType="lot_no"
                  />
                  <td>{item.TARIH || item.tarih || '-'}</td>
                  <td>{item.PERSONEL || item.personel || '-'}</td>
                  <td>{item.MAKINA || item.makina || '-'}</td>
                  <td>{(item.B_AGIRLIK || item.b_AGIRLIK || 0).toFixed(2)}</td>
                  <td>{(item.DARA || item.dara || 0).toFixed(2)}</td>
                  <td>{(item.NET || item.net || 0).toFixed(2)}</td>
                  <td>{(item.ADET || item.adet || 0).toLocaleString()}</td>
                </tr>
              )) : (
                <tr>
                  <td colSpan="12">Üretim bilgileri bulunamadı</td>
                </tr>
              )}
            </tbody>
          </table>
        )
      case 'sarf':
        return (
          <table className="results-table">
            <thead>
              <tr>
                <th>İş Emri No</th>
                <th>Stok Kodu</th>
                <th>Stok Adı</th>
                <th>Hammadde Lot</th>
                <th>Harcanan Miktar</th>
              </tr>
            </thead>
            <tbody>
              {isMaterialLoading ? (
                <tr>
                  <td colSpan="5" className="loading-cell">
                    <div className="loading-spinner"></div>
                    Sarf malzeme verileri yükleniyor...
                  </td>
                </tr>
              ) : materialConsumptionData.length > 0 ? (
                materialConsumptionData.map((item, index) => (
                  <tr key={index}>
                    <td>{item.VS_SERI_NO || item.vS_SERI_NO || '-'}</td>
                    <td>{item.VS_STOK_KODU || item.vS_STOK_KODU || '-'}</td>
                    <td>{item.STOK_ADI || item.stoK_ADI || '-'}</td>
                    <td>{item.HAMMADDE_LOT || item.hammaddE_LOT || '-'}</td>
                    <td>{(item.HARCANAN || item.harcanan || 0).toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</td>
                  </tr>
                ))
              ) : (
                <tr>
                  <td colSpan="5">
                    {searchValue ? 'Bu arama için sarf malzeme verisi bulunamadı.' : 'Sarf malzeme bilgileri için önce arama yapın ve bu tab\'a tıklayın.'}
                  </td>
                </tr>
              )}
              {materialConsumptionData.length > 0 && (
                <tr className="total-row">
                  <td colSpan="4"><strong>TOPLAM</strong></td>
                  <td><strong>
                    {materialConsumptionData
                      .reduce((total, item) => total + (item.HARCANAN || item.harcanan || 0), 0)
                      .toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
                    }
                  </strong></td>
                </tr>
              )}
            </tbody>
          </table>
        )
      default:
        // Gövde ve Kapak tab'ları için dinamik render
        if (activeTab.startsWith('govde-') || activeTab.startsWith('kapak-')) {
          const partData = partMaterialData[activeTab] || []
          const isGovde = activeTab.startsWith('govde-')
          const partIndex = parseInt(activeTab.split('-')[1])
          const partInfo = isGovde ? montajParts.govdeParts[partIndex] : montajParts.kapakParts[partIndex]

          return (
            <table className="results-table">
              <thead>
                <tr>
                  <th>İş Emri No</th>
                  <th>Stok Kodu</th>
                  <th>Stok Adı</th>
                  <th>Hammadde Lot</th>
                  <th>Harcanan Miktar</th>
                </tr>
              </thead>
              <tbody>
                {partData.length > 0 ? (
                  partData.map((item, index) => (
                    <tr key={index}>
                      <td>{item.isemrI_NO || item.ISEMRI_NO || item.VS_SERI_NO || item.vS_SERI_NO || '-'}</td>
                      <td>{item.VS_STOK_KODU || item.vS_STOK_KODU || '-'}</td>
                      <td>{item.STOK_ADI || item.stoK_ADI || '-'}</td>
                      <td>{item.HAMMADDE_LOT || item.hammaddE_LOT || '-'}</td>
                      <td>{(item.HARCANAN || item.harcanan || 0).toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</td>
                    </tr>
                  ))
                ) : (
                  <tr>
                    <td colSpan="5">
                      {partInfo ?
                        `${isGovde ? 'Gövde' : 'Kapak'} malzeme bilgileri yükleniyor... (İş Emri: ${partInfo.isemrI_NO || partInfo.ISEMRI_NO || 'Bilinmiyor'})` :
                        'Parça bilgisi bulunamadı.'
                      }
                    </td>
                  </tr>
                )}
                {partData.length > 0 && (
                  <tr className="total-row">
                    <td colSpan="4"><strong>TOPLAM</strong></td>
                    <td><strong>
                      {partData
                        .reduce((total, item) => total + (item.HARCANAN || item.harcanan || 0), 0)
                        .toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
                      }
                    </strong></td>
                  </tr>
                )}
              </tbody>
            </table>
          )
        }

        return <div>Bilinmeyen tab</div>
      case 'quick':
        return (
          <table className="results-table">
            <thead>
              <tr>
                <th>İş Emri No</th>
                <th>Stok Kodu</th>
                <th>Yapı Kodu</th>
                <th>Seri No</th>
                <th>Lot No</th>
                <th>Tarih</th>
                <th>Personel</th>
                <th>Makina</th>
                <th>Birim Ağırlık</th>
                <th>Dara</th>
                <th>Net</th>
                <th>Adet</th>
              </tr>
            </thead>
            <tbody>
              {quickSearchResults.length > 0 ? quickSearchResults.map((item, index) => (
                <tr key={index}>
                  <ClickableCell
                    value={item.isemrI_NO || item.ISEMRI_NO || '-'}
                    searchType="isemri_no"
                  />
                  <td>{item.stoK_KODU || item.STOK_KODU || '-'}</td>
                  <td>{item.yaP_KOD || item.YAP_KOD || '-'}</td>
                  <ClickableCell
                    value={item.serI_NO || item.SERI_NO || '-'}
                    searchType="seri_no"
                  />
                  <ClickableCell
                    value={item.loT_NO || item.LOT_NO || '-'}
                    searchType="lot_no"
                  />
                  <td>{item.tarih || item.TARIH || '-'}</td>
                  <td>{item.personel || item.PERSONEL || '-'}</td>
                  <td>{item.makina || item.MAKINA || '-'}</td>
                  <td>{(item.b_AGIRLIK || item.B_AGIRLIK || 0).toFixed(2)}</td>
                  <td>{(item.dara || item.DARA || 0).toFixed(2)}</td>
                  <td>{(item.net || item.NET || 0).toFixed(2)}</td>
                  <td>{(item.adet || item.ADET || 0).toLocaleString()}</td>
                </tr>
              )) : (
                <tr>
                  <td colSpan="12">
                    {isQuickSearching ? 'Veriler yükleniyor...' : 'Detay bilgileri bulunamadı'}
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        )
    }
  }

  const renderContent = () => {
    switch (activeMenu) {
      case 'home':
        return (
          <div className="dashboard">
            <div className="dashboard-header">
              <h2>Peksan İzlenebilirlik Sistemi</h2>

              <div className="dashboard-controls">
                <div className="auto-refresh-toggle">
                  <label>
                    <input
                      type="checkbox"
                      checked={autoRefresh}
                      onChange={(e) => setAutoRefresh(e.target.checked)}
                    />
                    Otomatik Yenileme (10dk)
                  </label>
                </div>

                <button
                  className="manual-refresh-btn"
                  onClick={() => loadDashboardData(true)}
                  disabled={isDashboardLoading}
                >
                  {isDashboardLoading ? 'Yenileniyor...' : 'Manuel Yenile'}
                </button>

                <div className="last-updated">
                  Son Güncelleme: {lastUpdated.toLocaleTimeString()}
                </div>
              </div>
            </div>

            <div className="stats-container">
              <div className="stat-card">
                <h3>Günlük Enjeksiyon Üretimi</h3>
                <div className="stat-value">{dashboardData.dailyEnjeksiyon.toLocaleString()}</div>
                <div className="stat-unit">Koli</div>
              </div>
              <div className="stat-card">
                <h3>Günlük Montaj Üretimi</h3>
                <div className="stat-value">{dashboardData.dailyMontaj.toLocaleString()}</div>
                <div className="stat-unit">Koli</div>
              </div>
              <div className="stat-card">
                <h3>Günlük Serigrafi Üretimi</h3>
                <div className="stat-value">{dashboardData.dailySerigrafi.toLocaleString()}</div>
                <div className="stat-unit">Koli</div>
              </div>
            </div>

            {/* Sunburst Chart */}
            <div className="chart-section">
              <ProductionSunburst />
            </div>
          </div>
        )
      case 'backward':
        return (
          <div className="backward-traceability">
            <h2>Geri Yönlü İzlenebilirlik</h2>

            {/* Arama Formu */}
            <div className="search-form">
              <div className="search-options">
                <h3>Arama Kriteri Seçin:</h3>
                <div className="radio-group">
                  <label className="radio-option">
                    <input
                      type="radio"
                      name="searchType"
                      value="isemri_no"
                      checked={searchType === 'isemri_no'}
                      onChange={(e) => handleSearchTypeChange(e.target.value)}
                    />
                    <span>İş Emri No</span>
                  </label>
                  <label className="radio-option">
                    <input
                      type="radio"
                      name="searchType"
                      value="seri_no"
                      checked={searchType === 'seri_no'}
                      onChange={(e) => handleSearchTypeChange(e.target.value)}
                    />
                    <span>Seri No</span>
                  </label>
                  <label className="radio-option">
                    <input
                      type="radio"
                      name="searchType"
                      value="lot_no"
                      checked={searchType === 'lot_no'}
                      onChange={(e) => handleSearchTypeChange(e.target.value)}
                    />
                    <span>Lot No</span>
                  </label>
                </div>
              </div>

              {searchType && (
                <div className="search-input-section">
                  <div className="input-group">
                    <label htmlFor="searchValue">
                      {searchType === 'isemri_no' && 'İş Emri Numarası:'}
                      {searchType === 'seri_no' && 'Seri Numarası:'}
                      {searchType === 'lot_no' && 'Lot Numarası:'}
                    </label>
                    <input
                      id="searchValue"
                      type="text"
                      value={searchValue}
                      onChange={(e) => setSearchValue(e.target.value)}
                      placeholder={`${searchType === 'isemri_no' ? 'İş emri' : searchType === 'seri_no' ? 'Seri' : 'Lot'} numarasını girin...`}
                      onKeyDown={(e) => e.key === 'Enter' && handleSearch()}
                    />
                  </div>
                  <div className="button-group">
                    <button
                      className="search-btn"
                      onClick={handleSearch}
                      disabled={isSearching || !searchValue.trim()}
                    >
                      {isSearching ? 'Aranıyor...' : 'Ara'}
                    </button>
                    <button
                      className="clear-btn"
                      onClick={clearSearch}
                    >
                      Temizle
                    </button>
                  </div>
                </div>
              )}
            </div>

            {/* Tab'lı Sonuçlar Bölümü */}
            {searchResults.length > 0 && (
              <div className="results-section">
                {/* Tab Başlıkları */}
                <div className="tab-headers">
                  <div className="tab-buttons">
                    <button
                      className={`tab-header ${activeTab === 'isemri' ? 'active' : ''}`}
                      onClick={() => handleTabChange('isemri')}
                    >
                      İş Emri Bilgileri
                    </button>
                    <button
                      className={`tab-header ${activeTab === 'uretim' ? 'active' : ''}`}
                      onClick={() => handleTabChange('uretim')}
                    >
                      Üretim Bilgileri
                    </button>
                    <button
                      className={`tab-header ${activeTab === 'sarf' ? 'active' : ''}`}
                      onClick={() => handleTabChange('sarf')}
                    >
                      Kullanılan Malzemeler
                    </button>
                    {currentMachineType === 'M' && (
                      <>
                        {/* Gövde Tab'ları */}
                        {montajParts.govdeParts.map((_, index) => (
                          <button
                            key={`govde-${index}`}
                            className={`tab-header ${activeTab === `govde-${index}` ? 'active' : ''}`}
                            onClick={() => handleTabChange(`govde-${index}`)}
                          >
                            {montajParts.govdeParts.length > 1 ? `Gövde ${index + 1}` : 'Gövde'}
                          </button>
                        ))}
                        {/* Kapak Tab'ları */}
                        {montajParts.kapakParts.map((_, index) => (
                          <button
                            key={`kapak-${index}`}
                            className={`tab-header ${activeTab === `kapak-${index}` ? 'active' : ''}`}
                            onClick={() => handleTabChange(`kapak-${index}`)}
                          >
                            {montajParts.kapakParts.length > 1 ? `Kapak ${index + 1}` : 'Kapak'}
                          </button>
                        ))}
                      </>
                    )}
                    {quickSearchResults.length > 0 && (
                      <button
                        className={`tab-header ${activeTab === 'quick' ? 'active' : ''}`}
                        onClick={() => handleTabChange('quick')}
                      >
                        Detay Görünümü {isQuickSearching && '(Yükleniyor...)'}
                      </button>
                    )}
                  </div>

                  {/* Excel Export Butonu */}
                  <button
                    className="excel-export-btn"
                    onClick={exportToExcel}
                    title="Excel halini al"
                  >
                    📊 Excel Halini Al
                  </button>
                </div>

                {/* Tab İçeriği */}
                <div className="tab-content">
                  <div className="table-container">
                    {renderTabContent()}
                  </div>
                </div>
              </div>
            )}
          </div>
        )
      case 'forward':
        return (
          <div className="content-page">
            <h2>İleri Yönlü İzlenebilirlik</h2>
            <p>Bu bölüm yakında geliştirilecek...</p>
          </div>
        )
      case 'certification':
        return (
          <div className="certification">
            <h2>Sertifikasyon</h2>

            {/* Arama Formu */}
            <div className="search-form">
              <div className="search-options">
                <h3>Arama Kriteri Seçin:</h3>
                <div className="radio-group">
                  <label className="radio-option">
                    <input
                      type="radio"
                      name="certSearchType"
                      value="seri_no"
                      checked={certSearchType === 'seri_no'}
                      onChange={(e) => setCertSearchType(e.target.value)}
                    />
                    <span>Seri No</span>
                  </label>
                  <label className="radio-option">
                    <input
                      type="radio"
                      name="certSearchType"
                      value="lot_no"
                      checked={certSearchType === 'lot_no'}
                      onChange={(e) => setCertSearchType(e.target.value)}
                    />
                    <span>Lot No</span>
                  </label>
                </div>
              </div>

              {certSearchType && (
                <div className="search-input-section">
                  <div className="input-group">
                    <label htmlFor="certSearchValue">
                      {certSearchType === 'seri_no' && 'Seri Numarası:'}
                      {certSearchType === 'lot_no' && 'Lot Numarası:'}
                    </label>
                    <input
                      id="certSearchValue"
                      type="text"
                      value={certSearchValue}
                      onChange={(e) => setCertSearchValue(e.target.value)}
                      placeholder={`${certSearchType === 'seri_no' ? 'Seri numarasını girin...' : 'Lot numarasını girin... (Birden fazla için virgülle ayırın)'}`}
                      onKeyDown={(e) => e.key === 'Enter' && handleCertSearch()}
                    />
                  </div>
                  <div className="button-group">
                    <button
                      className="search-btn"
                      onClick={handleCertSearch}
                      disabled={!certSearchValue.trim() || isCertSearching || isIlkNumuneSearching || isIsEmriSearching}
                    >
                      {(isCertSearching || isIlkNumuneSearching || isIsEmriSearching) ? 'Aranıyor...' : 'Ara'}
                    </button>
                    <button
                      className="clear-btn"
                      onClick={() => {
                        setCertSearchValue('')
                        setCertResults([])
                        setIlkNumuneResults([])
                        setIsEmriResults([])
                        setKutleDenkligiResults([])
                        setActiveKutleDenkligiTab('')
                      }}
                    >
                      Temizle
                    </button>
                  </div>
                </div>
              )}
            </div>

            {/* Tab'lı Sonuçlar Bölümü */}
            {(certResults.length > 0 || ilkNumuneResults.length > 0 || isEmriResults.length > 0) && (
              <div className="results-section">
                {/* Tab Başlıkları */}
                <div className="tab-headers">
                  <div className="tab-buttons">
                    <button
                      className={`tab-header ${activeCertTab === 'kalite-sertifikalari' ? 'active' : ''}`}
                      onClick={() => setActiveCertTab('kalite-sertifikalari')}
                    >
                      Kalite Sertifikaları
                    </button>
                    <button
                      className={`tab-header ${activeCertTab === 'ilk-numune' ? 'active' : ''}`}
                      onClick={() => setActiveCertTab('ilk-numune')}
                    >
                      İlk Numune
                    </button>
                    <button
                      className={`tab-header ${activeCertTab === 'is-emri' ? 'active' : ''}`}
                      onClick={() => setActiveCertTab('is-emri')}
                    >
                      İş Emri
                    </button>
                     <button
                      className={`tab-header ${activeCertTab === 'kutle-denkligi' ? 'active' : ''}`}
                      onClick={() => setActiveCertTab('kutle-denkligi')}
                    >
                      Kütle Denkliği
                    </button>
                  </div>
                </div>

                {/* Kalite Sertifikaları Tab İçeriği */}
                {activeCertTab === 'kalite-sertifikalari' && certResults.length > 0 && (
                  <div className="tab-content">
                    <h3>Kalite Sertifikaları Sonuçları</h3>
                <div className="table-container">
                  <table className="results-table">
                    <thead>
                      <tr>
                        <th>İş Emri No</th>
                       {/*   <th>Kod 3</th>
                        <th>Grup İsim</th>*/}
                        <th>Analiz Numarası</th>
                        <th>PDF İşlemleri</th>
                      </tr>
                    </thead>
                    <tbody>
                      {certResults.map((item, index) => (
                        <tr key={index}>
                          <td>{item.isemriNo || '-'}</td>
                        {/*}  <td>{item.kod3 || '-'}</td>
                          <td>{item.grupIsim || '-'}</td>*/}
                          <td>{item.analizeNumber || '-'}</td>
                          <td>
                            {item.analizeNumber ? (
                              <div className="pdf-actions">
                                <button
                                  className="pdf-preview-btn" 
                                  onClick={() => previewPDF(item.analizeNumber)}
                                  title="PDF Önizle"
                                >
                                  👁️ Önizle
                                </button>
                                <button
                                  className="pdf-download-btn"
                                  onClick={() => downloadPDF(item.analizeNumber)}
                                  title="PDF İndir"
                                >
                                  📄 İndir
                                </button>
                              </div>
                            ) : (
                              <span className="no-pdf">PDF Yok</span>
                            )}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
                  </div>
                )}

                {/* İlk Numune Tab İçeriği */}
                {activeCertTab === 'ilk-numune' && ilkNumuneResults.length > 0 && (
                  <div className="tab-content">
                    <h3>İlk Numune Sonuçları</h3>
                    <div className="table-container">
                      <table className="results-table">
                        <thead>
                          <tr>
                            <th>İş Emri No</th>
                            <th>Analiz Numarası</th>
                            <th>PDF İşlemleri</th>
                          </tr>
                        </thead>
                        <tbody>
                          {ilkNumuneResults.map((item, index) => (
                            <tr key={index}>
                              <td>{item.isemriNo || '-'}</td>
                              <td>{item.analizeNumber || '-'}</td>
                              <td>
                                {item.analizeNumber ? (
                                  <div className="pdf-actions">
                                    <button
                                      className="pdf-preview-btn"
                                      onClick={() => previewIlkNumunePDF(item.analizeNumber)}
                                      title="PDF Önizle"
                                    >
                                      👁️ Önizle
                                    </button>
                                    <button
                                      className="pdf-download-btn"
                                      onClick={() => downloadIlkNumunePDF(item.analizeNumber)}
                                      title="PDF İndir"
                                    >
                                      📄 İndir
                                    </button>
                                  </div>
                                ) : (
                                  <span className="no-pdf">PDF Yok</span>
                                )}
                              </td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  </div>
                )}

                {/* İş Emri Tab İçeriği */}
                {activeCertTab === 'is-emri' && isEmriResults.length > 0 && (
                  <div className="tab-content">
                    <h3>İş Emri Sonuçları</h3>
                    <div className="table-container">
                      <table className="results-table">
                        <thead>
                          <tr>
                            <th>İş Emri No</th>
                            <th>Stok Kodu</th>
                           {/* <th>TIID</th> */}
                            <th>İşlemler</th>
                          </tr>
                        </thead>
                        <tbody>
                          {isEmriResults.map((item, index) => (
                            <tr key={index}>
                              <td>{item.isemriNo || '-'}</td>
                              <td>{item.stokKodu || '-'}</td>
                              {/*<td>{item.tiid || '-'}</td> */}
                              <td> 
                                {item.url ? (
                                  <div className="url-actions">
                                    <button
                                      className="url-link-btn"
                                      onClick={() => window.open(item.url, '_blank')}
                                      title="İş Emri Detayını Aç"
                                    >
                                      🔗 Detay Görüntüle
                                    </button>
                                  </div>
                                ) : (
                                  <span className="no-url">URL Yok</span>
                                )}
                              </td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  </div>
                )}

                {/* Kütle Denkliği Tab İçeriği */}
                {activeCertTab === 'kutle-denkligi' && kutleDenkligiResults.length > 0 && (
                  <div className="tab-content">
                    <h3>Kütle Denkliği Sonuçları</h3>

                    {/* Alt Tab Başlıkları */}
                    <div className="tab-headers">
                      {/* Montaj ve Kütle Denkliği tablarını birleştir ve sırala */}
                      {[
                        ...montajResults.map(item => item.tabBaslik),
                        ...[...new Set(kutleDenkligiResults.map(item => item.tabBaslik))]
                      ]
                        .sort((a, b) => {
                          // Önce montaj tabları (Montaj-1, Montaj-2...)
                          const aMontaj = a.includes('Montaj')
                          const bMontaj = b.includes('Montaj')
                          if (aMontaj && !bMontaj) return -1
                          if (!aMontaj && bMontaj) return 1

                          // Montaj tabları kendi içinde sıralama
                          if (aMontaj && bMontaj) {
                            const aNum = parseInt(a.split('-')[1]) || 0
                            const bNum = parseInt(b.split('-')[1]) || 0
                            return aNum - bNum
                          }

                          // Kütle Denkliği tabları için kategori sıralaması (Üst Kapak önce, sonra Gövde)
                          const aCategory = a.includes('Üst Kapak') ? 0 : 1
                          const bCategory = b.includes('Üst Kapak') ? 0 : 1
                          if (aCategory !== bCategory) return aCategory - bCategory

                          // Aynı kategori içinde sayıya göre sıralama
                          const aNum = parseInt(a.split('-')[1]) || 0
                          const bNum = parseInt(b.split('-')[1]) || 0
                          return aNum - bNum
                        })
                        .map((tabBaslik, index) => (
                        <button
                          key={index}
                          className={`tab-header ${
                            activeKutleDenkligiTab === tabBaslik ? 'active' : ''
                          }`}
                          onClick={() => {
                            setActiveKutleDenkligiTab(tabBaslik)
                          }}
                        >
                          {tabBaslik}
                        </button>
                      ))}
                    </div>

                    {/* Alt Tab İçerikleri */}

                    {/* Montaj Tab İçerikleri */}
                    {montajResults.map((montajItem, index) => {
                      return activeKutleDenkligiTab === montajItem.tabBaslik && (
                        <div key={`montaj-${index}`} className="tab-content">
                          <h4>{montajItem.tabBaslik}</h4>

                          {/* Üretim Sayıları Tablosu */}
                          <div className="table-section">
                            <h5>ÜRETİM SAYILARI</h5>
                            <div className="table-container">
                              {isMontajUretimLoading[montajItem.tabBaslik] ? (
                                <div className="loading">Yükleniyor...</div>
                              ) : montajUretimData[montajItem.tabBaslik] && montajUretimData[montajItem.tabBaslik].length > 0 ? (
                                <>
                                  <table className="results-table">
                                    <thead>
                                      <tr>
                                        <th>İş Emri No</th>
                                        <th>Stok Kodu</th>
                                        <th>Adet</th>
                                        <th>Net</th>
                                        <th>Brüt</th>
                                        <th>Koli</th>
                                        <th>Üretim Tipi</th>
                                      </tr>
                                    </thead>
                                    <tbody>
                                      {montajUretimData[montajItem.tabBaslik].map((item, itemIndex) => (
                                        <tr key={itemIndex}>
                                          <td>{item.isemriNo || '-'}</td>
                                          <td>{item.stokKodu || '-'}</td>
                                          <td>{item.adet || 0}</td>
                                          <td>{item.net || 0}</td>
                                          <td>{item.brut || 0}</td>
                                          <td>{item.koli || 0}</td>
                                          <td>{item.uretimTipi || '-'}</td>
                                        </tr>
                                      ))}
                                    </tbody>
                                  </table>

                                  {/* Toplam Bilgileri */}
                                  <div className="summary-section" style={{ marginTop: '15px', padding: '10px', backgroundColor: '#f8f9fa', borderRadius: '5px' }}>
                                    {(() => {
                                      const totalAdet = montajUretimData[montajItem.tabBaslik]?.reduce((sum, item) => sum + (parseInt(item.adet) || 0), 0) || 0;
                                      return (
                                        <div style={{ display: 'flex', flexDirection: 'column', gap: '5px' }}>
                                          <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                                            <span><strong>Kullanılan Gövde Sayısı :</strong></span>
                                            <span><strong>{totalAdet.toLocaleString()}</strong></span>
                                          </div>
                                          <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                                            <span><strong>Üretilen Kapak Sayısı :</strong></span>
                                            <span><strong>{totalAdet.toLocaleString()}</strong></span>
                                          </div>
                                        </div>
                                      );
                                    })()}
                                  </div>
                                </>
                              ) : (
                                <div className="no-data">Veri bulunamadı</div>
                              )}
                              {!isMontajUretimLoading[montajItem.tabBaslik] && (!montajUretimData[montajItem.tabBaslik] || montajUretimData[montajItem.tabBaslik].length === 0) && (
                                <div className="load-data-section">
                                  <button
                                    className="load-data-btn"
                                    onClick={() => loadMontajUretimData(montajItem.isemriNo, montajItem.tabBaslik)}
                                  >
                                    Üretim Verilerini Yükle
                                  </button>
                                </div>
                              )}
                            </div>
                          </div>

                          {/* Kullanılan Sarf Malzemeler Tablosu */}
                          <div className="table-section">
                            <h5>KULLANILAN SARF MALZEMELER</h5>
                            <div className="table-container">
                              {isMontajSarfLoading[montajItem.tabBaslik] ? (
                                <div className="loading">Yükleniyor...</div>
                              ) : montajSarfData[montajItem.tabBaslik] && montajSarfData[montajItem.tabBaslik].length > 0 ? (
                                <table className="results-table">
                                  <thead>
                                    <tr>
                                      <th>Stok Kodu</th>
                                      <th>Stok Adı</th>
                                      <th>Harcanan</th>
                                    </tr>
                                  </thead>
                                  <tbody>
                                    {montajSarfData[montajItem.tabBaslik].map((item, itemIndex) => (
                                      <tr key={itemIndex}>
                                        <td>{item.stokKodu || '-'}</td>
                                        <td>{item.stokAdi || '-'}</td>
                                        <td>{item.harcanan || 0}</td>
                                      </tr>
                                    ))}
                                  </tbody>
                                </table>
                              ) : (
                                <div className="no-data">Veri bulunamadı</div>
                              )}
                              {!isMontajSarfLoading[montajItem.tabBaslik] && (!montajSarfData[montajItem.tabBaslik] || montajSarfData[montajItem.tabBaslik].length === 0) && (
                                <div className="load-data-section">
                                  <button
                                    className="load-data-btn"
                                    onClick={() => loadMontajSarfData(montajItem.isemriNo, montajItem.tabBaslik)}
                                  >
                                    Sarf Malzeme Verilerini Yükle
                                  </button>
                                </div>
                              )}
                            </div>
                          </div>
                        </div>
                      )
                    })}

                    {/* Kütle Denkliği Tab İçerikleri */}
                    {[...new Set(kutleDenkligiResults.map(item => item.tabBaslik))]
                      .sort((a, b) => {
                        // Önce kategori sıralaması (Üst Kapak önce, sonra Gövde)
                        const aCategory = a.includes('Üst Kapak') ? 0 : 1
                        const bCategory = b.includes('Üst Kapak') ? 0 : 1
                        if (aCategory !== bCategory) return aCategory - bCategory

                        // Aynı kategori içinde sayıya göre sıralama
                        const aNum = parseInt(a.split('-')[1]) || 0
                        const bNum = parseInt(b.split('-')[1]) || 0
                        return aNum - bNum
                      })
                      .map((tabBaslik, index) => {
                      const currentItem = kutleDenkligiResults.find(item => item.tabBaslik === tabBaslik)
                      const isemriNo = currentItem?.isemriNo

                      return activeKutleDenkligiTab === tabBaslik && (
                        <div key={index} className="tab-content">
                          <h4>{tabBaslik}</h4>

                          {/* Üretim Bilgileri Tablosu */}
                          <div className="table-section">
                            <h5>{tabBaslik.includes('Gövde') ? 'Gövde' : 'Üst Kapak'} Üretim Bilgileri</h5>
                            <div className="table-container">
                              {isKutleDenkligiUretimLoading[tabBaslik] ? (
                                <div className="loading">Yükleniyor...</div>
                              ) : kutleDenkligiUretimData[tabBaslik] && kutleDenkligiUretimData[tabBaslik].length > 0 ? (
                                <table className="results-table">
                                  <thead>
                                    <tr>
                                      <th>İş Emri No</th>
                                      <th>Stok Kodu</th>
                                      <th>Adet</th>
                                      <th>Net KG</th>
                                      <th>Brüt KG</th>
                                      <th>Koli Sayısı</th>
                                      <th>Üretim Tipi</th>
                                    </tr>
                                  </thead>
                                  <tbody>
                                    {kutleDenkligiUretimData[tabBaslik].map((item, itemIndex) => (
                                      <tr key={itemIndex}>
                                        <td>{item.isemriNo || '-'}</td>
                                        <td>{item.stokKodu || '-'}</td>
                                        <td>{item.adet || 0}</td>
                                        <td>{item.netKg || 0}</td>
                                        <td>{item.brutKg || 0}</td>
                                        <td>{item.koliSayisi || 0}</td>
                                        <td>{item.uretTip || '-'}</td>
                                      </tr>
                                    ))}
                                  </tbody>
                                </table>
                              ) : (
                                <div className="no-data">
                                  <button
                                    onClick={() => loadKutleDenkligiUretimData(isemriNo, tabBaslik)}
                                    className="load-data-btn"
                                  >
                                    Üretim Verilerini Yükle
                                  </button>
                                </div>
                              )}
                            </div>
                          </div>

                          {/* Hammadde Kullanımı Tablosu */}
                          <div className="table-section">
                            <h5>Hammadde Kullanımı</h5>
                            <div className="table-container">
                              {isKutleDenkligiHammaddeLoading[tabBaslik] ? (
                                <div className="loading">Yükleniyor...</div>
                              ) : kutleDenkligiHammaddeData[tabBaslik] && kutleDenkligiHammaddeData[tabBaslik].length > 0 ? (
                                <table className="results-table">
                                  <thead>
                                    <tr>
                                      <th>İş Emri No</th>
                                      <th>Stok Kodu</th>
                                      <th>Stok Adı</th>
                                      <th>Hammadde Lot</th>
                                      <th>Harcanan</th>
                                    </tr>
                                  </thead>
                                  <tbody>
                                    {kutleDenkligiHammaddeData[tabBaslik].map((item, itemIndex) => (
                                      <tr key={itemIndex}>
                                        <td>{item.isemriNo || '-'}</td>
                                        <td>{item.vsStokKodu || '-'}</td>
                                        <td>{item.stokAdi || '-'}</td>
                                        <td>{item.hammaddeLot || '-'}</td>
                                        <td>{item.harcanan || 0}</td>
                                      </tr>
                                    ))}
                                  </tbody>
                                </table>
                              ) : (
                                <div className="no-data">
                                  <button
                                    onClick={() => loadKutleDenkligiHammaddeData(isemriNo, tabBaslik)}
                                    className="load-data-btn"
                                  >
                                    Hammadde Verilerini Yükle
                                  </button>
                                </div>
                              )}
                            </div>
                          </div>
                        </div>
                      )
                    })}
                  </div>
                )}
              </div>
            )}
          </div>
        )
      default:
        return null
    }
  }

  // If not authenticated, show login page
  if (!isAuthenticated) {
    return <Login onLogin={handleLogin} />
  }

  return (
    <div className="app">
      <nav className={`sidebar ${isSidebarCollapsed ? 'collapsed' : ''}`}>
        <div className="sidebar-header">
          <div className="header-content">
            {!isSidebarCollapsed && (
              <>
                <h1>Peksan ERP</h1>
                <div className="user-info">
                  <span className="welcome-text">Hoş geldiniz, {currentUser}</span>
                  <button className="logout-button" onClick={handleLogout}>
                    Çıkış Yap
                  </button>
                </div>
              </>
            )}
          </div>
          <button className="sidebar-toggle" onClick={toggleSidebar}>
            <span className={`toggle-icon ${isSidebarCollapsed ? 'collapsed' : ''}`}>
              {isSidebarCollapsed ? '→' : '←'}
            </span>
          </button>
        </div>
        <ul className="nav-menu">
          <li>
            <button
              className={`nav-button ${activeMenu === 'home' ? 'active' : ''}`}
              onClick={() => setActiveMenu('home')}
              title={isSidebarCollapsed ? 'Dashboard' : ''}
            >
              <span className="nav-icon">🏠</span>
              {!isSidebarCollapsed && <span className="nav-text">Dashboard</span>}
            </button>
          </li>
          <li>
            <button
              className={`nav-button ${activeMenu === 'backward' ? 'active' : ''}`}
              onClick={() => setActiveMenu('backward')}
              title={isSidebarCollapsed ? 'Geri Yönlü İzlenebilirlik' : ''}
            >
              <span className="nav-icon">🔍</span>
              {!isSidebarCollapsed && <span className="nav-text">Geri Yönlü İzlenebilirlik</span>}
            </button>
          </li>
          <li>
            <button
              className={`nav-button ${activeMenu === 'forward' ? 'active' : ''}`}
              onClick={() => setActiveMenu('forward')}
              title={isSidebarCollapsed ? 'İleri Yönlü İzlenebilirlik' : ''}
            >
              <span className="nav-icon">🔎</span>
              {!isSidebarCollapsed && <span className="nav-text">İleri Yönlü İzlenebilirlik</span>}
            </button>
          </li>
          <li>
            <button
              className={`nav-button ${activeMenu === 'certification' ? 'active' : ''}`}
              onClick={() => setActiveMenu('certification')}
              title={isSidebarCollapsed ? 'Sertifikasyon' : ''}
            >
              <span className="nav-icon">📋</span>
              {!isSidebarCollapsed && <span className="nav-text">Sertifikasyon</span>}
            </button>
          </li>
        </ul>
      </nav>
      <main className={`main-content ${isSidebarCollapsed ? 'sidebar-collapsed' : ''}`}>
        {renderContent()}
      </main>
    </div>
  )
}

export default App
